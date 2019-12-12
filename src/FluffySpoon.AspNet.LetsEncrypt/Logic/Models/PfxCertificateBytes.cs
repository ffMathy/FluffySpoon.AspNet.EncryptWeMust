namespace FluffySpoon.AspNet.LetsEncrypt.Logic
{
    public class PfxCertificateBytes
    {
        public byte[] Data { get; }

        public PfxCertificateBytes(byte[] data)
        {
            Data = data;
        }
    }
}