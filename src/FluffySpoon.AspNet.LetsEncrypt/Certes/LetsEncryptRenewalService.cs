using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using FluffySpoon.AspNet.LetsEncrypt.Certificates;
using Microsoft.Extensions.Logging;
using static FluffySpoon.AspNet.LetsEncrypt.Certificates.CertificateRenewalStatus;

namespace FluffySpoon.AspNet.LetsEncrypt.Certes
{
	public class LetsEncryptRenewalService : ILetsEncryptRenewalService
	{
		private readonly ICertificateProvider _certificateProvider;
		private readonly IEnumerable<ICertificateRenewalLifecycleHook> _lifecycleHooks;
		private readonly ILogger<ILetsEncryptRenewalService> _logger;
		private readonly SemaphoreSlim _semaphoreSlim;
		private readonly LetsEncryptOptions _options;

		private Timer _timer;

		public LetsEncryptRenewalService(
			ICertificateProvider certificateProvider,
			IEnumerable<ICertificateRenewalLifecycleHook> lifecycleHooks,
			ILogger<ILetsEncryptRenewalService> logger,
			LetsEncryptOptions options)
		{
			_certificateProvider = certificateProvider;
			_lifecycleHooks = lifecycleHooks;
			_logger = logger;
			_options = options;
			_semaphoreSlim = new SemaphoreSlim(1);
		}

		internal static X509Certificate2 Certificate { get; private set; }
		
		public Uri LetsEncryptUri => _options.LetsEncryptUri;
		
		public async Task StartAsync(CancellationToken cancellationToken)
		{
			if (_options.TimeAfterIssueDateBeforeRenewal == null && _options.TimeUntilExpiryBeforeRenewal == null)
			{
				throw new InvalidOperationException(
					"Neither TimeAfterIssueDateBeforeRenewal nor TimeUntilExpiryBeforeRenewal have been set," +
					" which means that the LetsEncrypt certificate will never renew.");
			}

			foreach (var lifecycleHook in _lifecycleHooks)
				await lifecycleHook.OnStartAsync();

			TimeSpan ts = _options.StartUpMode == StartUpMode.Immediate ? TimeSpan.Zero : Timeout.InfiniteTimeSpan;
			_timer = new Timer(async state => await RunOnceWithErrorHandlingAsync(), null, ts, TimeSpan.FromHours(1));
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
				await RunOnceAsync();
				_timer?.Change(TimeSpan.FromHours(1), TimeSpan.FromHours(1));
			} catch (Exception e) when (_options.RenewalFailMode != RenewalFailMode.Unhandled) {
				_logger.LogWarning(e, $"Exception occured renewing certificates: '{e.Message}.'");
				if (_options.RenewalFailMode == RenewalFailMode.LogAndRetry) {
					_timer?.Change(TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
				}
			}
		}

		internal void RunNowManual(int delayMS = 0) {
			if (_options.StartUpMode != StartUpMode.Manual)
				throw new InvalidOperationException($"{nameof(RunNowManual)} can only be called when the {nameof(LetsEncryptOptions)}.{nameof(LetsEncryptOptions.StartUpMode)} property is set to {nameof(StartUpMode.Manual)}.");
			_timer?.Change(TimeSpan.FromMilliseconds(delayMS), TimeSpan.FromHours(1));
		}

		internal void RunNowDelayed(int delayMS = 0) {
			if (_options.StartUpMode == StartUpMode.Delayed)
				_timer?.Change(TimeSpan.FromMilliseconds(delayMS), TimeSpan.FromHours(1));
		}

		public void Dispose()
		{
			_timer?.Dispose();
		}
	}
}