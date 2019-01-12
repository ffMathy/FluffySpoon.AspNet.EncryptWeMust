using System;
using System.Threading.Tasks;

namespace FluffySpoon.AspNet.LetsEncrypt.Persistence
{
	public class CustomPersistenceStrategy: ICertificatePersistenceStrategy, IChallengePersistenceStrategy
	{
		private readonly Func<PersistenceType, byte[], Task> persistAsync;
		private readonly Func<PersistenceType, Task<byte[]>> retrieveAsync;

		public CustomPersistenceStrategy(
			Func<PersistenceType, byte[], Task> persistAsync,
			Func<PersistenceType, Task<byte[]>> retrieveAsync)
		{
			this.persistAsync = persistAsync;
			this.retrieveAsync = retrieveAsync;
		}

		public Task PersistAsync(PersistenceType persistenceType, byte[] bytes)
		{
			return persistAsync(persistenceType, bytes);
		}

		public Task<byte[]> RetrieveAsync(PersistenceType persistenceType)
		{
			return retrieveAsync(persistenceType);
		}
	}
}
