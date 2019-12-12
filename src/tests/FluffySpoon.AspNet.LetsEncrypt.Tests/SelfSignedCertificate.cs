using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace FluffySpoon.AspNet.LetsEncrypt.Tests
{
    public static class SelfSignedCertificate
    {
        public static X509Certificate2 Make(DateTime from, DateTime to)
        {
            var ecdsa = ECDsa.Create(); // generate asymmetric key pair
            var req = new CertificateRequest("cn=foobar", ecdsa, HashAlgorithmName.SHA256);
            var cert = req.CreateSelfSigned(@from, to);
            return cert;
        }
    }
}