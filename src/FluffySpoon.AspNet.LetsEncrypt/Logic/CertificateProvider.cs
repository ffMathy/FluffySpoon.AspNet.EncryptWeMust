using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using FluffySpoon.AspNet.LetsEncrypt.Persistence;
using Microsoft.Extensions.Logging;

namespace FluffySpoon.AspNet.LetsEncrypt.Logic
{
    public interface ICertificateProvider
    {
        Task<CertificateRenewalResult> GetCertificate(X509Certificate2 current = null);
    }
    
    public class CertificateProvider : ICertificateProvider
    {
        private readonly IPersistenceService _persistenceService;
        private readonly INewCertificate _newCertificate;
        private readonly ICertificateValidator _certificateValidator;
        private readonly ILogger<CertificateProvider> _logger;

        public CertificateProvider(
            IPersistenceService persistenceService,
            INewCertificate newCertificate,
            ICertificateValidator certificateValidator,
            ILogger<CertificateProvider> logger)
        {
            _persistenceService = persistenceService;
            _newCertificate = newCertificate;
            _certificateValidator = certificateValidator;
            _logger = logger;
        }

        public async Task<CertificateRenewalResult> GetCertificate(X509Certificate2 current = null)
        {
            _logger.LogInformation("Checking to see if in-memory LetsEncrypt certificate needs renewal.");
            if (_certificateValidator.IsCertificateValid(current))
            {
                _logger.LogInformation("Current in-memory LetsEncrypt certificate is valid.");
                return new CertificateRenewalResult(current, CertificateRenewalStatus.Unchanged);
            }
			
            _logger.LogInformation("Checking to see if existing LetsEncrypt certificate has been persisted and is valid.");
            var persistedSiteCertificate = await _persistenceService.GetPersistedSiteCertificateAsync();
            if (_certificateValidator.IsCertificateValid(persistedSiteCertificate))
            {
                _logger.LogInformation("A persisted non-expired LetsEncrypt certificate was found and will be used.");
                return new CertificateRenewalResult(persistedSiteCertificate, CertificateRenewalStatus.LoadedFromStore);
            }
			
            _logger.LogInformation("No valid certificate was found. Requesting new certificate from LetsEncrypt.");
            var newCertificate = await _newCertificate.RequestNewLetsEncryptCertificate();
            return new CertificateRenewalResult(newCertificate, CertificateRenewalStatus.Renewed);
        }
    }
}