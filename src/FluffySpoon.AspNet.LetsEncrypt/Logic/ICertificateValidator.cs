using System.Security.Cryptography.X509Certificates;

namespace FluffySpoon.AspNet.LetsEncrypt.Logic
{
    public interface ICertificateValidator
    {
        bool IsCertificateValid(X509Certificate2 certificate);
    }
}