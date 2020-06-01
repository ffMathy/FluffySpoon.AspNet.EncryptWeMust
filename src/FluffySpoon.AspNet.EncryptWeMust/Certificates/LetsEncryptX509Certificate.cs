using System;
using System.Security.Cryptography.X509Certificates;

namespace FluffySpoon.AspNet.EncryptWeMust.Certificates
{
    public class LetsEncryptX509Certificate : IPersistableCertificate
    {
        readonly X509Certificate2 _certificate;

        public LetsEncryptX509Certificate(X509Certificate2 certificate)
        {
            _certificate = certificate;
            RawData = certificate.RawData;
        }

        public LetsEncryptX509Certificate(byte[] data)
        {
            _certificate = new X509Certificate2(data, nameof(FluffySpoon));
            RawData = data;
        }

        public DateTime NotAfter => _certificate.NotAfter;
        public DateTime NotBefore => _certificate.NotBefore;
        public string Thumbprint => _certificate.Thumbprint;
        public X509Certificate2 GetCertificate() => _certificate;
        public byte[] RawData { get; }

        public override string ToString()
        {
            return _certificate.ToString();
        }
    }
}