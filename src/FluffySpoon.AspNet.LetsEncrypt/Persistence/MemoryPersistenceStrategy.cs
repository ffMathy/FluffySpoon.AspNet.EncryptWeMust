using System.Collections.Generic;
using System.Threading.Tasks;

namespace FluffySpoon.AspNet.LetsEncrypt.Persistence
{
	class MemoryPersistenceStrategy : ICertificatePersistenceStrategy, IChallengePersistenceStrategy
	{
		private IDictionary<string, byte[]> bytes;

		public MemoryPersistenceStrategy()
		{
			bytes = new Dictionary<string, byte[]>();
		}

		public Task PersistAsync(string key, byte[] bytes)
		{
			if (this.bytes.ContainsKey(key))
			{
				if (bytes == null)
				{
					this.bytes.Remove(key);
				}
				else
				{
					this.bytes[key] = bytes;
				}
			} else {
				if(bytes == null)
					return Task.CompletedTask;
				
				this.bytes.Add(key, bytes);
			}

			return Task.CompletedTask;
		}

		public Task<byte[]> RetrieveAsync(string key)
		{
			if(bytes.ContainsKey(key))
				return Task.FromResult(bytes[key]);

			return Task.FromResult<byte[]>(null);
		}
	}
}
