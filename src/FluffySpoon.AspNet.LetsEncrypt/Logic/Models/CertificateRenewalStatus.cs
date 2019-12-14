namespace FluffySpoon.AspNet.LetsEncrypt.Logic.Models
{
    public enum CertificateRenewalStatus
    {
        Unchanged,
        LoadedFromStore,
        Renewed
    }
}