using System;
using System.Threading.Tasks;

namespace FluffySpoon.AspNet.LetsEncrypt.Persistence
{
	public class CustomPersistenceStrategy: ICertificatePersistenceStrategy, IChallengePersistenceStrategy
	{
		private readonly Func<string, byte[], Task> persistAsync;
		private readonly Func<string, Task<byte[]>> retrieveAsync;

		public CustomPersistenceStrategy(
			Func<string, byte[], Task> persistAsync,
			Func<string, Task<byte[]>> retrieveAsync)
		{
			this.persistAsync = persistAsync;
			this.retrieveAsync = retrieveAsync;
		}

		public Task PersistAsync(string key, byte[] bytes)
		{
			return persistAsync(key, bytes);
		}

		public Task<byte[]> RetrieveAsync(string key)
		{
			return retrieveAsync(key);
		}
	}
}
