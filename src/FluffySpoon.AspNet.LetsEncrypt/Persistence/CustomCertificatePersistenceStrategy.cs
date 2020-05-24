using System;
using System.Threading.Tasks;
using FluffySpoon.AspNet.LetsEncrypt.Certificates;

namespace FluffySpoon.AspNet.LetsEncrypt.Persistence
{
	public class CustomCertificatePersistenceStrategy : ICertificatePersistenceStrategy
	{
		private readonly Func<CertificateType, byte[], Task> persistAsync;
		private readonly Func<CertificateType, Task<byte[]>> retrieveAsync;

		public CustomCertificatePersistenceStrategy(
			Func<CertificateType, byte[], Task> persistAsync,
			Func<CertificateType, Task<byte[]>> retrieveAsync)
		{
			this.persistAsync = persistAsync;
			this.retrieveAsync = retrieveAsync;
		}

		public Task PersistAsync(CertificateType persistenceType, IPersistableCertificate certificate)
		{
			return persistAsync(persistenceType, certificate.RawData);
		}

		public async Task<IKeyCertificate> RetrieveAccountCertificateAsync()
		{
			return new AccountKeyCertificate(await retrieveAsync(CertificateType.Account));
		}

		public async Task<IAbstractCertificate> RetrieveSiteCertificateAsync()
		{
			return new LetsEncryptX509Certificate(await retrieveAsync(CertificateType.Site));
		}
	}
}
