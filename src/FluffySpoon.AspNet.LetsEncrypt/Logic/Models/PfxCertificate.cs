namespace FluffySpoon.AspNet.LetsEncrypt.Logic.Models
{
    public class PfxCertificate
    {
        public byte[] Bytes { get; }

        public PfxCertificate(byte[] bytes)
        {
            Bytes = bytes;
        }
    }
}