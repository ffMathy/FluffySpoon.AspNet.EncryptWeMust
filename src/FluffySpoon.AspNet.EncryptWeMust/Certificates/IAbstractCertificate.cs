using System;

namespace FluffySpoon.AspNet.EncryptWeMust.Certificates
{
    /// <summary>
    /// The most generic form of certificate, metadata provision only
    /// </summary>
    public interface IAbstractCertificate
    {
        public DateTime NotAfter { get; }
        public DateTime NotBefore { get; }
        string Thumbprint { get; }
    }
}