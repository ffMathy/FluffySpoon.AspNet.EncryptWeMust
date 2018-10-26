using System;
using System.Threading.Tasks;

namespace FluffySpoon.AspNet.LetsEncrypt
{
	public class CustomCertificatePersistenceStrategy: ICertificatePersistenceStrategy
	{
		private readonly Func<byte[], Task> persistAsync;
		private readonly Func<Task<byte[]>> retrieveAsync;

		public CustomCertificatePersistenceStrategy(
			Func<byte[], Task> persistAsync,
			Func<Task<byte[]>> retrieveAsync)
		{
			this.persistAsync = persistAsync;
			this.retrieveAsync = retrieveAsync;
		}

		public Task PersistAsync(byte[] certificateBytes)
		{
			return persistAsync(certificateBytes);
		}

		public Task<byte[]> RetrieveAsync()
		{
			return retrieveAsync();
		}
	}
}
