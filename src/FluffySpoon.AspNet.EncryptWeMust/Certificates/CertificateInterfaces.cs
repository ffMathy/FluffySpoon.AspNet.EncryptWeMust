using Certes;

namespace FluffySpoon.AspNet.LetsEncrypt.Certificates
{
    /// <summary>
    /// A certificate which can be persisted as a stream of bytes
    /// </summary>
    public interface IPersistableCertificate : IAbstractCertificate
    {
        public byte[] RawData { get; }
    }
    
    /// <summary>
    /// A certificate which can return an IKey
    /// </summary>
    public interface IKeyCertificate
    {
        IKey Key { get; }
    }
}