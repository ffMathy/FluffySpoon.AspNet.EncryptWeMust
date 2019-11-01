using System;
using System.Threading.Tasks;

namespace FluffySpoon.AspNet.LetsEncrypt.Persistence
{
	public class CustomDnsChallengePersistenceStrategy : IDnsChallengePersistenceStrategy
	{
		public delegate Task PersistDelegate(string recordName, string recordType, string recordValue);
		public delegate Task DeleteDelegate(string recordName, string recordType, string recordValue);

		private readonly PersistDelegate _persistAsync;
		private readonly DeleteDelegate _deleteAsync;

		public CustomDnsChallengePersistenceStrategy(
			PersistDelegate persistAsync,
			DeleteDelegate deleteAsync)
		{
			this._persistAsync = persistAsync;
			this._deleteAsync = deleteAsync;
		}

		public Task DeleteAsync(string recordName, string recordType, string recordValue)
		{
			return _deleteAsync(recordName, recordType, recordValue);
		}

		public Task PersistAsync(string recordName, string recordType, string recordValue)
		{
			return _persistAsync(recordName, recordType, recordValue);
		}
	}
}
