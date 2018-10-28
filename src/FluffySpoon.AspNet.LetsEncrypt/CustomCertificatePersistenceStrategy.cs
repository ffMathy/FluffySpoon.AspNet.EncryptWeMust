using System;
using System.Threading.Tasks;

namespace FluffySpoon.AspNet.LetsEncrypt
{
	public class CustomCertificatePersistenceStrategy: ICertificatePersistenceStrategy
	{
		private readonly Func<string, byte[], Task> persistAsync;
		private readonly Func<string, Task<byte[]>> retrieveAsync;

		public CustomCertificatePersistenceStrategy(
			Func<string, byte[], Task> persistAsync,
			Func<string, Task<byte[]>> retrieveAsync)
		{
			this.persistAsync = persistAsync;
			this.retrieveAsync = retrieveAsync;
		}

		public Task PersistAsync(string key, byte[] certificateBytes)
		{
			return persistAsync(key, certificateBytes);
		}

		public Task<byte[]> RetrieveAsync(string key)
		{
			return retrieveAsync(key);
		}
	}
}
