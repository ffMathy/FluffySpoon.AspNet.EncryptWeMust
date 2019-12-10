using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace FluffySpoon.AspNet.LetsEncrypt
{
    public interface ICertificateRenewal
    {
        Task<X509Certificate2> RenewCertificateIfNeeded(X509Certificate2 current);
        Uri LetsEncryptUri { get; }
    }
}