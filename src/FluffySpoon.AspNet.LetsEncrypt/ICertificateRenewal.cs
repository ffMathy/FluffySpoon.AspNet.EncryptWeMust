using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace FluffySpoon.AspNet.LetsEncrypt
{
    public enum CertificateRenewalStatus
    {
        Unchanged,
        LoadedFromStore,
        Renewed
    }
    
    public interface ICertificateRenewal
    {
        Task<(X509Certificate2, CertificateRenewalStatus)> RenewCertificateIfNeeded(X509Certificate2 current);
    }
}