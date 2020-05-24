namespace FluffySpoon.AspNet.LetsEncrypt.Certificates
{
    public class CertificateRenewalResult
    {
        public CertificateRenewalResult(IAbstractCertificate certificate, CertificateRenewalStatus status)
        {
            Certificate = certificate;
            Status = status;
        }

        public IAbstractCertificate Certificate { get; }
        
        public CertificateRenewalStatus Status { get; }
    }
}