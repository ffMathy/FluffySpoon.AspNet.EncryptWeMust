using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using FluffySpoon.AspNet.LetsEncrypt.Certificates;

namespace FluffySpoon.AspNet.LetsEncrypt.Tests
{
    public static class SelfSignedCertificate
    {
        public static LetsEncryptX509Certificate Make(DateTime from, DateTime to)
        {
            var ecdsa = ECDsa.Create(); // generate asymmetric key pair
            var req = new CertificateRequest("cn=foobar", ecdsa, HashAlgorithmName.SHA256);
            var cert = req.CreateSelfSigned(from, to);
            return new LetsEncryptX509Certificate(cert);
        }
    }
}