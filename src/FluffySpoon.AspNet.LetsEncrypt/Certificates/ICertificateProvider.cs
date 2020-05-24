using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace FluffySpoon.AspNet.LetsEncrypt.Certificates
{
    public interface ICertificateProvider
    {
        Task<CertificateRenewalResult> RenewCertificateIfNeeded(IAbstractCertificate current = null);
    }
}