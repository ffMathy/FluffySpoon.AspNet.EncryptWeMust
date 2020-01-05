using System;
using System.Collections.Generic;
using System.Text;
using FluffySpoon.AspNet.LetsEncrypt.Certes;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Options;

namespace FluffySpoon.AspNet.LetsEncrypt
{
    internal class KestrelOptionsSetup : IConfigureOptions<KestrelServerOptions>
    {
        public void Configure(KestrelServerOptions options)
        {
            options.ConfigureHttpsDefaults(o =>
            {
                o.ServerCertificateSelector = (_a, _b) => LetsEncryptRenewalService.Certificate;
            });
        }
    }
}
