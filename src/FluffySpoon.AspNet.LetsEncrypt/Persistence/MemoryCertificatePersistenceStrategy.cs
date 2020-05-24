using System;
using System.Threading.Tasks;
using FluffySpoon.AspNet.LetsEncrypt.Certificates;

namespace FluffySpoon.AspNet.LetsEncrypt.Persistence
{
	class MemoryCertificatePersistenceStrategy : ICertificatePersistenceStrategy
	{
		IKeyCertificate _accountCertificate;
		IAbstractCertificate _siteCertificate;

		public Task PersistAsync(CertificateType persistenceType, IPersistableCertificate certificate)
		{
			switch (persistenceType)
			{
				case CertificateType.Account:
					_accountCertificate = (IKeyCertificate)certificate;
					break;
				case CertificateType.Site:
					_siteCertificate = certificate;
					break;
				default:
					throw new ArgumentException("Unhandled persitence type", nameof(persistenceType));
			}
			return Task.CompletedTask;
		}

		public Task<IKeyCertificate> RetrieveAccountCertificateAsync()
		{
			return Task.FromResult(_accountCertificate);
		}

		public Task<IAbstractCertificate> RetrieveSiteCertificateAsync()
		{
			return Task.FromResult(_siteCertificate);
		}
	}
}
