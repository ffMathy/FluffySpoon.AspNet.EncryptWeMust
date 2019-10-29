using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace FluffySpoon.AspNet.LetsEncrypt.Persistence
{
	public interface IDnsChallengePersistenceStrategy
	{
		/// <summary>
		/// The async method to use for persisting a DNS challenge.
		/// </summary>
		Task PersistAsync(string recordName, string recordType, string recordValue);

		/// <summary>
		/// Optional. The async method to use for deleting a DNS challenge after validation has completed.
		/// </summary>
		Task DeleteAsync(string recordName, string recordType);
	}
}
