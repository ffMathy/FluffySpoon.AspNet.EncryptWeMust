using System;
using System.Text;
using Certes;

namespace FluffySpoon.AspNet.LetsEncrypt.Certificates
{
    /// <summary>
    /// The type of certificate used to store a Let's Encrypt account key
    /// </summary>
    public class AccountKeyCertificate : IPersistableCertificate, IKeyCertificate
    {
        public AccountKeyCertificate(IKey key)
        {
            Key = key;
            var text = key.ToPem();
            RawData = Encoding.UTF8.GetBytes(text);
        }

        public AccountKeyCertificate(byte[] bytes)
        {
            RawData = bytes;
            var text = Encoding.UTF8.GetString(bytes);
            Key = KeyFactory.FromPem(text);
        }

        public DateTime NotAfter => throw new InvalidOperationException("No metadata available for key certificate");
        public DateTime NotBefore => throw new InvalidOperationException("No metadata available for key certificate");
        public string Thumbprint => throw new InvalidOperationException("No metadata available for key certificate");

        public byte[] RawData { get; }
        public IKey Key { get; }
    }
}