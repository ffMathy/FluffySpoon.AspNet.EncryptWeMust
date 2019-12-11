using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace FluffySpoon.AspNet.LetsEncrypt.Logic
{
    public interface ILetsEncryptClient
    {
        Task<CertificateRenewalResult> AttemptCertificateRenewal(X509Certificate2 current);
    }
}