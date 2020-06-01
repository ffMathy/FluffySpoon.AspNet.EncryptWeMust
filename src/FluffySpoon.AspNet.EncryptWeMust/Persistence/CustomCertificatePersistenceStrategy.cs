using System;
using System.Threading.Tasks;
using FluffySpoon.AspNet.EncryptWeMust.Certificates;

namespace FluffySpoon.AspNet.EncryptWeMust.Persistence
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
			byte[] bytes = await retrieveAsync(CertificateType.Account);
			if (bytes == null)
			{
				return null;
			}
			return new AccountKeyCertificate(bytes);
		}

		public async Task<IAbstractCertificate> RetrieveSiteCertificateAsync()
		{
			byte[] bytes = await retrieveAsync(CertificateType.Account);
			if (bytes == null)
			{
				return null;
			}
			return new LetsEncryptX509Certificate(bytes);
		}
	}
}
