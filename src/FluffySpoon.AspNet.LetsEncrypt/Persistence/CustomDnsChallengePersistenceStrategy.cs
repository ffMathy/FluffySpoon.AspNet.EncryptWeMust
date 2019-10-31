using System;
using System.Threading.Tasks;

namespace FluffySpoon.AspNet.LetsEncrypt.Persistence
{
	public class CustomDnsChallengePersistenceStrategy : IDnsChallengePersistenceStrategy
	{
		private readonly Func<string, string, string, Task> persistAsync;
		private readonly Func<string, string, Task> deleteAsync;

		public CustomDnsChallengePersistenceStrategy(
			Func<string, string, string, Task> persistAsync,
			Func<string, string, Task> deleteAsync)
		{
			this.persistAsync = persistAsync;
			this.deleteAsync = deleteAsync;
		}

		public Task DeleteAsync(string recordName, string recordType)
		{
			return deleteAsync(recordName, recordType);
		}

		public Task PersistAsync(string recordName, string recordType, string recordValue)
		{
			return persistAsync(recordName, recordType, recordValue);
		}
	}
}
