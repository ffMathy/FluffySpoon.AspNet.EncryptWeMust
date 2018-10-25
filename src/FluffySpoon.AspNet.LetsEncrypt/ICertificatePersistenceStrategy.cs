using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace FluffySpoon.AspNet.LetsEncrypt
{
	public interface ICertificatePersistenceStrategy
	{
		/// <summary>
		/// Optional. The async method to use for persisting a generated certificate for later use (if server restarts).
		/// </summary>
		Task PersistAsync(byte[] certificateBytes);

		/// <summary>
		/// Optional. The async method to use for fetching a previously generated certificate.
		/// </summary>
		Task<byte[]> RetrieveAsync();
	}
}
