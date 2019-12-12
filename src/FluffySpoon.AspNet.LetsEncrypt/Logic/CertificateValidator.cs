using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;

namespace FluffySpoon.AspNet.LetsEncrypt.Logic
{
    public class CertificateValidator : ICertificateValidator
    {
        private readonly LetsEncryptOptions _options;
        private readonly ILogger<CertificateValidator> _logger;

        public CertificateValidator(
            LetsEncryptOptions options,
            ILogger<CertificateValidator> logger)
        {
            _options = options;
            _logger = logger;
        }

        public bool IsCertificateValid(X509Certificate2 certificate)
        {
            try
            {
                if (certificate == null)
                    return false;
                else
                {
                    var now = DateTime.Now;
                    
                    if (_options.TimeUntilExpiryBeforeRenewal != null && certificate.NotAfter - now < _options.TimeUntilExpiryBeforeRenewal)
                        return false;
                    else if (_options.TimeAfterIssueDateBeforeRenewal != null && now - certificate.NotBefore > _options.TimeAfterIssueDateBeforeRenewal)
                        return false;
                    else if (certificate.NotBefore > now || certificate.NotAfter < now)
                        return false;
                    else
                        return true;
                }
            }
            catch (CryptographicException exc)
            {
                _logger.LogError(exc, "Exception occured during certificate validation");
                return false;
            }
        }
    }
}