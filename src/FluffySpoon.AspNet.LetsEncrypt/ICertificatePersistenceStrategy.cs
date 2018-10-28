using System.Threading.Tasks;

namespace FluffySpoon.AspNet.LetsEncrypt
{
	public interface ICertificatePersistenceStrategy
	{
		/// <summary>
		/// Optional. The async method to use for persisting a generated certificate for later use (if server restarts).
		/// </summary>
		Task PersistAsync(string key, byte[] certificateBytes);

		/// <summary>
		/// Optional. The async method to use for fetching a previously generated certificate.
		/// </summary>
		Task<byte[]> RetrieveAsync(string key);
	}
}
