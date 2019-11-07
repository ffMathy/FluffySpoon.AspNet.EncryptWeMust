using System.Collections.Generic;
using System.Threading.Tasks;

namespace FluffySpoon.AspNet.LetsEncrypt.Persistence
{
	class MemoryCertificatePersistenceStrategy : ICertificatePersistenceStrategy
	{
		private IDictionary<CertificateType, byte[]> bytes;

		public MemoryCertificatePersistenceStrategy()
		{
			bytes = new Dictionary<CertificateType, byte[]>();
		}

		public Task PersistAsync(CertificateType persistenceType, byte[] bytes)
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

		public Task<byte[]> RetrieveAsync(CertificateType persistenceType)
		{
			if(bytes.ContainsKey(persistenceType))
				return Task.FromResult(bytes[persistenceType]);

			return Task.FromResult<byte[]>(null);
		}
	}
}
