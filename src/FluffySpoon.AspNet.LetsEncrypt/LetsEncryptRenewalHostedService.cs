using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
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
		private readonly ICertificatePersistenceStrategy _certificatePersistenceStrategy;
		private readonly ILogger<LetsEncryptRenewalHostedService> _logger;
		private readonly LetsEncryptOptions _options;
		private readonly LetsEncryptCertificateContainer _stateContainer;
		private readonly SemaphoreSlim _semaphoreSlim;

		private IAcmeContext acme;

		private Timer _timer;

		public LetsEncryptRenewalHostedService(
			ICertificatePersistenceStrategy certificatePersistenceStrategy,
			ILogger<LetsEncryptRenewalHostedService> logger,
			LetsEncryptOptions options,
			LetsEncryptCertificateContainer stateContainer)
		{
			_certificatePersistenceStrategy = certificatePersistenceStrategy;
			_logger = logger;
			_options = options;
			_stateContainer = stateContainer;

			_semaphoreSlim = new SemaphoreSlim(1);
		}

		public void Dispose()
		{
			_timer?.Dispose();
		}

		public Task StartAsync(CancellationToken cancellationToken)
		{
			_timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromHours(1));
			return Task.CompletedTask;
		}

		public Task StopAsync(CancellationToken cancellationToken)
		{
			_timer?.Change(Timeout.Infinite, 0);
			return Task.CompletedTask;
		}

		private async void DoWork(object state)
		{
			if(_semaphoreSlim.CurrentCount == 0)
				return;

			await _semaphoreSlim.WaitAsync();
			try
			{
				if (IsCertificateValid)
					return;

				if (_stateContainer.Certificate == null)
				{
					var certificateBytes = await _certificatePersistenceStrategy.RetrieveAsync();
					if (certificateBytes != null)
					{
						_stateContainer.Certificate = GetCertificateFromBytes(certificateBytes);

						if (IsCertificateValid)
						{
							_logger.LogInformation("A persisted non-expired LetsEncrypt certificate was found and will be used.");
							return;
						}

						_logger.LogInformation("A persisted but expired LetsEncrypt certificate was found and will be renewed.");
					}
				}

				var letsencryptUri = _options.UseStaging ?
					WellKnownServers.LetsEncryptStagingV2 :
					WellKnownServers.LetsEncryptV2;
				if (acme == null)
				{
					_logger.LogDebug("Creating LetsEncrypt account with email {0}.", _options.Email);

					acme = new AcmeContext(letsencryptUri);
					await acme.NewAccount(_options.Email, true);
				}

				var domains = _options.Domains.ToArray();
				_logger.LogInformation("Ordering LetsEncrypt certificate for domains {0}.", domains);

				var order = await acme.NewOrder(domains);
				var allAuthorizations = await order.Authorizations();
				var challengeContexts = await Task.WhenAll(
					allAuthorizations.Select(x => x.Http()));

				_logger.LogInformation("Validating all pending order authorizations.");

				_stateContainer.PendingChallengeContexts = challengeContexts;

				await Task.WhenAll(
					challengeContexts.Select(x => x.Validate()));

				_logger.LogInformation("Acquiring certificate through signing request.");

				var privateKey = KeyFactory.NewKey(KeyAlgorithm.ES256);
				var certificateChain = await order.Generate(
					_options.CertificateSigningRequest,
					privateKey);

				var pfxBuilder = certificateChain.ToPfx(privateKey);
				var pfxBytes = pfxBuilder.Build("LetsEncrypt", string.Empty);

				_logger.LogInformation("Certificate acquired.");

				await _certificatePersistenceStrategy.PersistAsync(pfxBytes);

				_logger.LogInformation("Certificate persisted for later use.");

				_stateContainer.Certificate = GetCertificateFromBytes(pfxBytes);
			}
			finally
			{
				_semaphoreSlim.Release();
			}
		}

		private bool IsCertificateValid =>
			_stateContainer.Certificate != null &&
			_stateContainer.Certificate.NotAfter - DateTime.Now > _options.TimeUntilExpiryBeforeRenewal;

		private static X509Certificate2 GetCertificateFromBytes(byte[] pfxBytes)
		{
			return new X509Certificate2(pfxBytes);
		}
	}
}
