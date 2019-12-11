using System;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using FluffySpoon.AspNet.LetsEncrypt.Persistence;
using Microsoft.Extensions.Logging;
using static FluffySpoon.AspNet.LetsEncrypt.Logic.CertificateRenewalStatus;

namespace FluffySpoon.AspNet.LetsEncrypt.Logic
{
	public class LetsEncryptFacade : ILetsEncryptFacade
	{
		private readonly IPersistenceService _persistenceService;
		private readonly IAcmeAuthenticator _authenticator;
		private readonly ILetsEncryptClient _client;
		private readonly ILogger<ILetsEncryptFacade> _logger;
		private readonly LetsEncryptOptions _options;

		public LetsEncryptFacade(
			LetsEncryptOptions options,
			IPersistenceService persistenceService,
			IAcmeAuthenticator authenticator,
			ILetsEncryptClient client,
			ILogger<LetsEncryptFacade> logger)
		{
			_persistenceService = persistenceService;
			_authenticator = authenticator;
			_client = client;
			_logger = logger;
			_options = options;
		}

        public async Task<CertificateRenewalResult> AttemptCertificateRenewal(X509Certificate2? current)
		{
			_logger.LogInformation("Checking to see if in-memory LetsEncrypt certificate needs renewal.");

			if (IsCertificateValid(current))
			{
				_logger.LogInformation("Current in-memory LetsEncrypt certificate is valid.");
				return new CertificateRenewalResult(current, Unchanged);
			}
			
			_logger.LogInformation("Checking to see if existing LetsEncrypt certificate has been persisted and is valid.");

			var persistedSiteCertificate = await _persistenceService.GetPersistedSiteCertificateAsync();
			if (IsCertificateValid(persistedSiteCertificate))
			{
				_logger.LogInformation("A persisted non-expired LetsEncrypt certificate was found and will be used.");
				return new CertificateRenewalResult(persistedSiteCertificate, LoadedFromStore);
			}
			
			_logger.LogInformation("No valid certificate was found. Requesting new certificate from LetsEncrypt.");
			
			var acme = await _authenticator.AuthenticateAsync();
			
			var domains = _options.Domains?.ToArray() ?? Array.Empty<string>();
			
			var placedOrder = await _client.PlaceOrder(domains, acme);

			await _persistenceService.PersistChallengesAsync(placedOrder.ChallengeDtos);

			try
			{
				var pfxCertificateBytes = await _client.ValidateOrder(placedOrder);

				await _persistenceService.PersistSiteCertificateAsync(pfxCertificateBytes.Data);

				var newCertificate = new X509Certificate2(pfxCertificateBytes.Data, nameof(FluffySpoon));
				
				return new CertificateRenewalResult(newCertificate, Renewed);
			}
			finally
			{
				await _persistenceService.DeleteChallengesAsync(placedOrder.ChallengeDtos);
			}
		}

        private bool IsCertificateValid(X509Certificate2 certificate)
        {
	        try
	        {
		        if (certificate == null)
			        return false;
		        
		        var now = DateTime.Now;
			        
		        if (_options.TimeUntilExpiryBeforeRenewal != null && certificate.NotAfter - now < _options.TimeUntilExpiryBeforeRenewal)
			        return false;
		        
		        if (_options.TimeAfterIssueDateBeforeRenewal != null && now - certificate.NotBefore > _options.TimeAfterIssueDateBeforeRenewal)
			        return false;
		        
		        if (certificate.NotBefore > now || certificate.NotAfter < now)
			        return false;
		        
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