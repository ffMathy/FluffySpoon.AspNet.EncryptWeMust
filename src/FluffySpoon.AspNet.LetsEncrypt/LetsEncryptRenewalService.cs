using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace FluffySpoon.AspNet.LetsEncrypt
{
	public class LetsEncryptRenewalService : ILetsEncryptRenewalService
	{
		private readonly ICertificateRenewal _logic;
		private readonly IEnumerable<ICertificateRenewalLifecycleHook> _lifecycleHooks;
		private readonly ILogger<ILetsEncryptRenewalService> _logger;
		private readonly SemaphoreSlim _semaphoreSlim;
		private readonly LetsEncryptOptions _options;

		private Timer _timer;

		public LetsEncryptRenewalService(
			ICertificateRenewal logic,
			IEnumerable<ICertificateRenewalLifecycleHook> lifecycleHooks,
			ILogger<ILetsEncryptRenewalService> logger,
			LetsEncryptOptions options)
		{
			_logic = logic;
			_lifecycleHooks = lifecycleHooks;
			_logger = logger;
			_options = options;
			_semaphoreSlim = new SemaphoreSlim(1);
		}

		public static X509Certificate2 Certificate { get; private set; }
		
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
			{
				await lifecycleHook.OnStartAsync();
			}

			_timer = new Timer(
				callback: async state => await RunOnceWithErrorHandlingAsync(), 
				state: null,
				dueTime: TimeSpan.Zero, 
				period: TimeSpan.FromHours(1));
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
				Certificate = await _logic.RenewCertificateIfNeeded(Certificate);
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
			try
			{
				await RunOnceAsync();
				_timer?.Change(Timeout.InfiniteTimeSpan, TimeSpan.FromHours(1));
			}
			catch (Exception e) when (_options.RenewalFailMode != RenewalFailMode.Unhandled)
			{
				_logger.LogWarning(e, $"Exception occured renewing certificates: '{e.Message}.'");
				if (_options.RenewalFailMode == RenewalFailMode.LogAndRetry)
					_timer?.Change(Timeout.InfiniteTimeSpan, TimeSpan.FromMinutes(1));
			}
		}

		public void Dispose()
		{
			_timer?.Dispose();
		}
	}
}