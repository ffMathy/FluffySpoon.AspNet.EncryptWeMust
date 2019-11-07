using System.Threading.Tasks;

namespace FluffySpoon.AspNet.LetsEncrypt.Persistence
{
	public interface ICertificatePersistenceStrategy
	{
		/// <summary>
		/// Optional. The async method to use for persisting some data for later use (if server restarts).
		/// </summary>
		Task PersistAsync(CertificateType persistenceType, byte[] bytes);

		/// <summary>
		/// Optional. The async method to use for fetching previously generated data for a given key.
		/// </summary>
		Task<byte[]> RetrieveAsync(CertificateType persistenceType);
	}
}
