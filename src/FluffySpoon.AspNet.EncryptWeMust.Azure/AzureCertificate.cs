using System;
using FluffySpoon.AspNet.EncryptWeMust.Certificates;
using Microsoft.Azure.Management.AppService.Fluent;

namespace FluffySpoon.AspNet.EncryptWeMust.Azure
{
    /// <summary>
    /// The representation of the kind of metadata-only certificate which Azure Appservices can provide via the API
    /// </summary>
    public class AzureCertificate : IAbstractCertificate
    {
        readonly IAppServiceCertificate _certificate;

        public AzureCertificate(IAppServiceCertificate certificate)
        {
            _certificate = certificate;
        }

        public DateTime NotAfter => _certificate.ExpirationDate;
        public DateTime NotBefore => _certificate.IssueDate;
        public string Thumbprint => _certificate.Thumbprint;

        public override string ToString()
        {
            return $"Azure-{Thumbprint}: From {NotBefore} until {NotAfter}";
        }
    }
}