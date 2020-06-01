using System;
using System.Collections.Generic;
using System.Text;
using FluffySpoon.AspNet.EncryptWeMust.Certes;
using FluffySpoon.AspNet.EncryptWeMust.Certificates;
using FluffySpoon.AspNet.EncryptWeMust.Persistence;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FluffySpoon.AspNet.EncryptWeMust
{
    internal class KestrelOptionsSetup : IConfigureOptions<KestrelServerOptions>
    {
        readonly ILogger<KestrelOptionsSetup> _logger;

        public KestrelOptionsSetup(ILogger<KestrelOptionsSetup> logger)
        {
            _logger = logger;
        }
        
        public void Configure(KestrelServerOptions options)
        {
            if (LetsEncryptRenewalService.Certificate is LetsEncryptX509Certificate x509Certificate)
            {
                options.ConfigureHttpsDefaults(o =>
                {
                    o.ServerCertificateSelector = (_a, _b) => x509Certificate.GetCertificate();
                });
            }
            else if(LetsEncryptRenewalService.Certificate != null)
            {
                _logger.LogError("This certificate cannot be used with Kestrel");
            }
        }
    }
}
