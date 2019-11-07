using System;
using System.Threading.Tasks;

namespace FluffySpoon.AspNet.LetsEncrypt.Persistence
{
	public class CustomCertificatePersistenceStrategy: ICertificatePersistenceStrategy
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

		public Task PersistAsync(CertificateType persistenceType, byte[] bytes)
		{
			return persistAsync(persistenceType, bytes);
		}

		public Task<byte[]> RetrieveAsync(CertificateType persistenceType)
		{
			return retrieveAsync(persistenceType);
		}
	}
}
