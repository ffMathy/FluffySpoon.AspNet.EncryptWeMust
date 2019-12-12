namespace FluffySpoon.AspNet.LetsEncrypt.Logic
{
    public enum CertificateRenewalStatus
    {
        Unchanged,
        LoadedFromStore,
        Renewed
    }
}