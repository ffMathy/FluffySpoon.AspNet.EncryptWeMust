using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Certes;
using Certes.Acme;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FluffySpoon.AspNet.LetsEncrypt
{
	class LetsEncryptRenewalHostedService : IHostedService, IDisposable
	{
		private readonly IEnumerable<ICertificatePersistenceStrategy> _certificatePersistenceStrategies;
		private readonly IEnumerable<ICertificateRenewalLifecycleHook> _lifecycleHooks;
		private readonly ILogger<LetsEncryptRenewalHostedService> _logger;
		private readonly LetsEncryptOptions _options;
		private readonly LetsEncryptCertificateContainer _stateContainer;
		private readonly SemaphoreSlim _semaphoreSlim;

		private IAcmeContext acme;

		private Timer _timer;

		public LetsEncryptRenewalHostedService(
			IEnumerable<ICertificatePersistenceStrategy> certificatePersistenceStrategies,
			IEnumerable<ICertificateRenewalLifecycleHook> lifecycleHooks,
			ILogger<LetsEncryptRenewalHostedService> logger,
			LetsEncryptOptions options,
			LetsEncryptCertificateContainer stateContainer)
		{
			_certificatePersistenceStrategies = certificatePersistenceStrategies;
			_lifecycleHooks = lifecycleHooks;
			_logger = logger;
			_options = options;
			_stateContainer = stateContainer;

			_semaphoreSlim = new SemaphoreSlim(1);
		}

		public void Dispose()
		{
			_logger.LogWarning("The LetsEncrypt middleware's background renewal thread is shutting down.");
			_timer?.Dispose();
		}

		public async Task StartAsync(CancellationToken cancellationToken)
		{
			if (_options.TimeAfterIssueDateBeforeRenewal == null && _options.TimeUntilExpiryBeforeRenewal == null)
				throw new InvalidOperationException(
					"Neither TimeAfterIssueDateBeforeRenewal nor TimeUntilExpiryBeforeRenewal have been set, which means that the LetsEncrypt certificate will never renew.");

			foreach (var lifecycleHook in _lifecycleHooks)
				await lifecycleHook.OnStartAsync();

			_timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromHours(1));
		}

		public async Task StopAsync(CancellationToken cancellationToken)
		{
			_timer?.Change(Timeout.Infinite, 0);

			foreach (var lifecycleHook in _lifecycleHooks)
				await lifecycleHook.OnStopAsync();
		}

		private async Task<byte[]> GetPersistedCertificateBytesAsync()
		{
			foreach (var strategy in _certificatePersistenceStrategies)
			{
				var bytes = await strategy.RetrieveAsync();
				if (bytes != null)
					return bytes;
			}

			return null;
		}

		private async Task<bool> TryRetrievingValidPersistedCertificate()
		{
			if (_stateContainer.Certificate != null)
				return false;

			var certificateBytes = await GetPersistedCertificateBytesAsync();
			if (certificateBytes == null)
				return false;

			_stateContainer.Certificate = GetCertificateFromBytes(certificateBytes);

			if (IsCertificateValid)
			{
				_logger.LogInformation("A persisted non-expired LetsEncrypt certificate was found and will be used.");
				return true;
			}

			_logger.LogInformation("A persisted but expired LetsEncrypt certificate was found and will be renewed.");

			return false;
		}

		private async void DoWork(object state)
		{
			if (_semaphoreSlim.CurrentCount == 0)
				return;

			await _semaphoreSlim.WaitAsync();
			try
			{
				if (IsCertificateValid)
					return;

				if (await TryRetrievingValidPersistedCertificate())
					return;

				var letsencryptUri = _options.UseStaging
					? WellKnownServers.LetsEncryptStagingV2
					: WellKnownServers.LetsEncryptV2;
				if (acme == null)
				{
					_logger.LogDebug("Creating LetsEncrypt account with email {0}.", _options.Email);

					acme = new AcmeContext(letsencryptUri);
					await acme.NewAccount(_options.Email, true);
				}

				var domains = _options.Domains.ToArray();
				_logger.LogInformation("Ordering LetsEncrypt certificate for domains {0}.", new object[] {domains});

				var order = await acme.NewOrder(domains);
				var allAuthorizations = await order.Authorizations();
				var challengeContexts = await Task.WhenAll(
					allAuthorizations.Select(x => x.Http()));

				_logger.LogInformation("Validating all pending order authorizations.");

				_stateContainer.PendingChallengeContexts = challengeContexts;

				await Task.WhenAll(
					challengeContexts.Select(x => x.Validate()));

				_stateContainer.PendingChallengeContexts = null;

				_logger.LogInformation("Acquiring certificate through signing request.");

				var privateKey = KeyFactory.NewKey(KeyAlgorithm.ES256);
				var certificateChain = await order.Generate(
					_options.CertificateSigningRequest,
					privateKey);

				var pfxBuilder = certificateChain.ToPfx(privateKey);
				var pfxBytes = pfxBuilder.Build("LetsEncrypt", string.Empty);

				_logger.LogInformation("Certificate acquired.");

				var persistenceTasks = _certificatePersistenceStrategies.Select(x => x.PersistAsync(pfxBytes));
				await Task.WhenAll(persistenceTasks);

				_logger.LogInformation("Certificate persisted for later use.");

				_stateContainer.Certificate = GetCertificateFromBytes(pfxBytes);
			} catch(Exception ex)
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

		private bool IsCertificateValid =>
			_stateContainer.Certificate != null &&
			((_options.TimeUntilExpiryBeforeRenewal == null || _stateContainer.Certificate.NotAfter - DateTime.Now >
			_options.TimeUntilExpiryBeforeRenewal) &&
			(_options.TimeAfterIssueDateBeforeRenewal == null || DateTime.Now - _stateContainer.Certificate.NotBefore >
			_options.TimeAfterIssueDateBeforeRenewal));

		private static X509Certificate2 GetCertificateFromBytes(byte[] pfxBytes)
		{
			return new X509Certificate2(pfxBytes);
		}
	}
}