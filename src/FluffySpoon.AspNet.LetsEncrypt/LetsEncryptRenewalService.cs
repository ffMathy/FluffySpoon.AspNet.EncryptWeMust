using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Certes;
using Certes.Acme;
using Certes.Acme.Resource;
using FluffySpoon.AspNet.LetsEncrypt.Exceptions;
using FluffySpoon.AspNet.LetsEncrypt.Persistence;
using FluffySpoon.AspNet.LetsEncrypt.Persistence.Models;
using Microsoft.Extensions.Logging;

namespace FluffySpoon.AspNet.LetsEncrypt
{
	public class LetsEncryptRenewalService : ILetsEncryptRenewalService
	{
		public const string CertificateFriendlyName = "FluffySpoonAspNetLetsEncryptCertificate";

		private readonly IEnumerable<ICertificateRenewalLifecycleHook> _lifecycleHooks;
		private readonly IPersistenceService _persistenceService;
		private readonly ILogger<ILetsEncryptRenewalService> _logger;

		private readonly LetsEncryptOptions _options;
		private readonly SemaphoreSlim _semaphoreSlim;

		private IAcmeContext acme;

		private Timer _timer;

		public static X509Certificate2 Certificate { get; private set; }

		public Uri LetsEncryptUri => _options.UseStaging
			? WellKnownServers.LetsEncryptStagingV2
			: WellKnownServers.LetsEncryptV2;

		public LetsEncryptRenewalService(
			IEnumerable<ICertificateRenewalLifecycleHook> lifecycleHooks,
			IPersistenceService persistenceService,
			ILogger<ILetsEncryptRenewalService> logger,
			LetsEncryptOptions options)
		{
			_lifecycleHooks = lifecycleHooks;
			_persistenceService = persistenceService;
			_logger = logger;
			_options = options;

			_semaphoreSlim = new SemaphoreSlim(1);
		}

		public void Dispose()
		{
			_timer?.Dispose();
		}

		public async Task StartAsync(CancellationToken cancellationToken)
		{
			if (_options.TimeAfterIssueDateBeforeRenewal == null && _options.TimeUntilExpiryBeforeRenewal == null)
				throw new InvalidOperationException(
					"Neither TimeAfterIssueDateBeforeRenewal nor TimeUntilExpiryBeforeRenewal have been set, which means that the LetsEncrypt certificate will never renew.");

			foreach (var lifecycleHook in _lifecycleHooks)
				await lifecycleHook.OnStartAsync();

			_timer = new Timer(async (state) => await RunOnceWithErrorHandlingAsync(), null, TimeSpan.Zero, TimeSpan.FromHours(1));
		}

		public async Task StopAsync(CancellationToken cancellationToken)
		{
			_logger.LogWarning("The LetsEncrypt middleware's background renewal thread is shutting down.");
			_timer?.Change(Timeout.Infinite, 0);

			foreach (var lifecycleHook in _lifecycleHooks)
				await lifecycleHook.OnStopAsync();
		}

		private async Task<bool> TryRetrievingValidPersistedSiteCertificateAsync()
		{
			if (Certificate != null)
				return false;

			var certificate = await _persistenceService.GetPersistedSiteCertificateAsync();
			if (certificate == null)
				return false;

			Certificate = certificate;

			if (IsCertificateValid)
			{
				_logger.LogInformation("A persisted non-expired LetsEncrypt certificate was found and will be used.");
				return true;
			}

			_logger.LogInformation("A persisted but expired LetsEncrypt certificate was found and will be renewed.");

			return false;
		}

		private async Task RunOnceWithErrorHandlingAsync()
		{
			try
			{
				await RunOnceAsync();
				_timer.Change(Timeout.InfiniteTimeSpan, TimeSpan.FromHours(1));
			}
			catch (Exception e) when (_options.RenewalFailMode != RenewalFailMode.Unhandled)
			{
				_logger.LogWarning(e, $"Exception occured renewing certificates: '{e.Message}.'");
				if (_options.RenewalFailMode == RenewalFailMode.LogAndRetry)
					_timer.Change(Timeout.InfiniteTimeSpan, TimeSpan.FromMinutes(1));
			}
		}

		public async Task RunOnceAsync()
		{
			if (_semaphoreSlim.CurrentCount == 0)
				return;

			await _semaphoreSlim.WaitAsync();

			_logger.LogInformation("Checking to see if in-memory LetsEncrypt certificate needs renewal.");

			try
			{
				if (IsCertificateValid)
					return;

				_logger.LogInformation("Checking to see if existing LetsEncrypt certificate has been persisted and is valid.");

				var alreadyHasValidCertificate = await TryRetrievingValidPersistedSiteCertificateAsync();
				if (alreadyHasValidCertificate)
					return;

				await AuthenticateAsync();

				var domains = _options.Domains?.ToArray() ?? Array.Empty<string>();
				await AcquireNewCertificateForDomains(domains);
			}
			catch (Exception ex)
			{
				foreach (var lifecycleHook in _lifecycleHooks)
					await lifecycleHook.OnExceptionAsync(ex);

				throw;
			}
			finally
			{
				_semaphoreSlim.Release();
			}
		}

		private async Task AcquireNewCertificateForDomains(string[] domains)
		{
			_logger.LogInformation("Ordering LetsEncrypt certificate for domains {0}.", new object[] { domains });

			var order = await acme.NewOrder(domains);
			await ValidateOrderAsync(order);

			var certificateBytes = await AcquireCertificateBytesFromOrderAsync(order);
			if (certificateBytes == null)
			{
				throw new InvalidOperationException("The certificate from the order was null.");
			}

			await _persistenceService.PersistSiteCertificateAsync(certificateBytes);

			Certificate = new X509Certificate2(certificateBytes, nameof(FluffySpoon));
		}

		private async Task AuthenticateAsync()
		{
			if (acme != null)
				return;

			var existingAccountKey = await _persistenceService.GetPersistedAccountCertificateAsync();
			if (existingAccountKey != null)
			{
				await UseExistingLetsEncryptAccount(existingAccountKey);
			}
			else
			{
				await CreateNewLetsEncryptAccount();
			}
		}

		private async Task UseExistingLetsEncryptAccount(IKey existingAccountKey)
		{
			_logger.LogDebug("Using existing LetsEncrypt account.");

			acme = new AcmeContext(LetsEncryptUri, existingAccountKey);
			await acme.Account();
		}

		private async Task CreateNewLetsEncryptAccount()
		{
			_logger.LogDebug("Creating LetsEncrypt account with email {0}.", _options.Email);

			acme = new AcmeContext(LetsEncryptUri);
			await acme.NewAccount(_options.Email, true);

			await _persistenceService.PersistAccountCertificateAsync(acme.AccountKey);
		}

		private async Task<byte[]> AcquireCertificateBytesFromOrderAsync(IOrderContext order)
		{
			_logger.LogInformation("Acquiring certificate through signing request.");

			var privateKey = KeyFactory.NewKey(KeyAlgorithm.ES256);
			var certificateChain = await order.Generate(
				_options.CertificateSigningRequest,
				privateKey);

			var pfxBuilder = certificateChain.ToPfx(privateKey);
			pfxBuilder.FullChain = true;

			var pfxBytes = pfxBuilder.Build(CertificateFriendlyName, nameof(FluffySpoon));

			_logger.LogInformation("Certificate acquired.");

			return pfxBytes;
		}

		private async Task ValidateOrderAsync(IOrderContext order)
		{
			var allAuthorizations = await order.Authorizations();
			var challengeContexts = await Task.WhenAll(
				allAuthorizations.Select(x => x.Http()));

			_logger.LogInformation("Validating all pending order authorizations.");

			var challengeDtos = challengeContexts.Select(x => new ChallengeDto()
			{
				Token = x.Token,
				Response = x.KeyAuthz
			}).ToArray();
			await _persistenceService.PersistChallengesAsync(challengeDtos);

			try
			{
				var challenges = await ValidateChallengesAsync(challengeContexts);
				var challengeExceptions = challenges
					.Where(x => x.Status == ChallengeStatus.Invalid)
					.Select(x => new Exception(x.Error.Type + ": " + x.Error.Detail))
					.ToArray();
				if (challengeExceptions.Length > 0)
					throw new OrderInvalidException(
						"One or more LetsEncrypt orders were invalid. Make sure that LetsEncrypt can contact the domain you are trying to request an SSL certificate for, in order to verify it.",
						new AggregateException(challengeExceptions));
			}
			finally
			{
				await _persistenceService.PersistChallengesAsync(null);
			}
		}

		private static async Task<Challenge[]> ValidateChallengesAsync(IChallengeContext[] challengeContexts)
		{
			var challenges = await Task.WhenAll(
								challengeContexts.Select(x => x.Validate()));

			while (true)
			{
				if (!challenges.Any(x => x.Status == ChallengeStatus.Pending))
					break;

				await Task.Delay(1000);
				challenges = await Task.WhenAll(challengeContexts.Select(x => x.Resource()));
			}

			return challenges;
		}

		private bool IsCertificateValid
		{
			get
			{
				try
				{
					if (Certificate == null)
						return false;
					else if (_options.TimeUntilExpiryBeforeRenewal != null && Certificate.NotAfter - DateTime.Now < _options.TimeUntilExpiryBeforeRenewal)
						return false;
					else if (_options.TimeAfterIssueDateBeforeRenewal != null && DateTime.Now - Certificate.NotBefore > _options.TimeAfterIssueDateBeforeRenewal)
						return false;
					else if (Certificate.NotBefore > DateTime.Now || Certificate.NotAfter < DateTime.Now)
						return false;
					else
						return true;
				}
				catch (CryptographicException exc)
				{
					_logger.LogError(exc, "Exception occured during certificate validation");
					return false;
				}
			}
		}

	}
}