using System.Collections.Generic;
using System.Threading.Tasks;

namespace FluffySpoon.AspNet.LetsEncrypt.Persistence
{
	class MemoryPersistenceStrategy : ICertificatePersistenceStrategy, IChallengePersistenceStrategy
	{
		private IDictionary<PersistenceType, byte[]> bytes;

		public MemoryPersistenceStrategy()
		{
			bytes = new Dictionary<PersistenceType, byte[]>();
		}

		public Task PersistAsync(PersistenceType persistenceType, byte[] bytes)
		{
			if (this.bytes.ContainsKey(persistenceType))
			{
				if (bytes == null)
				{
					this.bytes.Remove(persistenceType);
				}
				else
				{
					this.bytes[persistenceType] = bytes;
				}
			} else {
				if(bytes == null)
					return Task.CompletedTask;
				
				this.bytes.Add(persistenceType, bytes);
			}

			return Task.CompletedTask;
		}

		public Task<byte[]> RetrieveAsync(PersistenceType persistenceType)
		{
			if(bytes.ContainsKey(persistenceType))
				return Task.FromResult(bytes[persistenceType]);

			return Task.FromResult<byte[]>(null);
		}
	}
}
