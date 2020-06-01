using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using FluffySpoon.AspNet.EncryptWeMust.Certificates;
using FluffySpoon.AspNet.EncryptWeMust.Persistence;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using static FluffySpoon.AspNet.EncryptWeMust.Certificates.CertificateRenewalStatus;

namespace FluffySpoon.AspNet.EncryptWeMust.Certes
{
	public class LetsEncryptRenewalService : ILetsEncryptRenewalService
	{
		private readonly ICertificateProvider _certificateProvider;
		private readonly IEnumerable<ICertificateRenewalLifecycleHook> _lifecycleHooks;
		private readonly ILogger<ILetsEncryptRenewalService> _logger;
		private readonly IHostApplicationLifetime _lifetime;
		private readonly SemaphoreSlim _semaphoreSlim;
		private readonly LetsEncryptOptions _options;

		private Timer _timer;

		public LetsEncryptRenewalService(
			ICertificateProvider certificateProvider,
			IEnumerable<ICertificateRenewalLifecycleHook> lifecycleHooks,
			IHostApplicationLifetime lifetime,
			ILogger<ILetsEncryptRenewalService> logger,
			LetsEncryptOptions options)
		{
			_certificateProvider = certificateProvider;
			_lifecycleHooks = lifecycleHooks;
			_lifetime = lifetime;
			_logger = logger;
			_options = options;
			_semaphoreSlim = new SemaphoreSlim(1);
		}

		internal static IAbstractCertificate Certificate { get; private set; }
		
		public Uri LetsEncryptUri => _options.LetsEncryptUri;
		
		public async Task StartAsync(CancellationToken cancellationToken)
		{
			if (_options.TimeAfterIssueDateBeforeRenewal == null && _options.TimeUntilExpiryBeforeRenewal == null)
			{
				throw new InvalidOperationException(
					"Neither TimeAfterIssueDateBeforeRenewal nor TimeUntilExpiryBeforeRenewal have been set," +
					" which means that the LetsEncrypt certificate will never renew.");
			}

			_logger.LogTrace("LetsEncryptRenewalService StartAsync");

			foreach (var lifecycleHook in _lifecycleHooks)
				await lifecycleHook.OnStartAsync();

			_timer = new Timer(async state => await RunOnceWithErrorHandlingAsync(), null, Timeout.InfiniteTimeSpan, TimeSpan.FromHours(1));
			
			_lifetime.ApplicationStarted.Register(() => OnApplicationStarted(cancellationToken));
		}

		public async Task StopAsync(CancellationToken cancellationToken)
		{
			_logger.LogWarning("The LetsEncrypt middleware's background renewal thread is shutting down.");
			_timer?.Change(Timeout.Infinite, 0);

			foreach (var lifecycleHook in _lifecycleHooks)
				await lifecycleHook.OnStopAsync();
		}

		public async Task RunOnceAsync()
		{
			if (_semaphoreSlim.CurrentCount == 0)
				return;

			await _semaphoreSlim.WaitAsync();

			try
			{
				var result = await _certificateProvider.RenewCertificateIfNeeded(Certificate);

				if (result.Status != Unchanged)
				{
					// Preload intermediate certs before exposing certificate to the Kestrel
					using var chain = new X509Chain
					{
						ChainPolicy =
						{
							RevocationMode = X509RevocationMode.NoCheck
						}
					};

					if (result.Certificate is LetsEncryptX509Certificate x509cert)
					{
						if (chain.Build(x509cert.GetCertificate()))
						{
							_logger.LogInformation("Successfully built certificate chain");
						}
						else
						{
							_logger.LogWarning(
								"Was not able to build certificate chain. This can cause an outage of your app.");
						}
					}
				}

				Certificate = result.Certificate;
				
				if (result.Status == Renewed)
				{
					foreach (var lifecycleHook in _lifecycleHooks)
						await lifecycleHook.OnRenewalSucceededAsync();
				}
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

		private async Task RunOnceWithErrorHandlingAsync()
		{
			try {
				_logger.LogTrace("LetsEncryptRenewalService - timer callback starting");
				await RunOnceAsync();
				_timer?.Change(TimeSpan.FromHours(1), TimeSpan.FromHours(1));
			} catch (Exception e) when (_options.RenewalFailMode != RenewalFailMode.Unhandled) {
				_logger.LogWarning(e, "Exception occurred renewing certificates: '{Message}'", e.Message);
				if (_options.RenewalFailMode == RenewalFailMode.LogAndRetry) {
					_timer?.Change(TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
				}
			}
		}

		private void OnApplicationStarted(CancellationToken t) {
			_logger.LogInformation("LetsEncryptRenewalService - Application started");
			_timer?.Change(_options.RenewalServiceStartupDelay, TimeSpan.FromHours(1));
		}

		public void Dispose()
		{
			_timer?.Dispose();
		}
	}
}