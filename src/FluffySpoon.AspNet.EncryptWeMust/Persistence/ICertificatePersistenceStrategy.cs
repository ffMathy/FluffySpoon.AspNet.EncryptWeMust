using System.Threading.Tasks;
using FluffySpoon.AspNet.EncryptWeMust.Certificates;

namespace FluffySpoon.AspNet.EncryptWeMust.Persistence
{
	public interface ICertificatePersistenceStrategy
	{
		/// <summary>
		/// Optional. The async method to use for persisting some data for later use (if server restarts).
		/// </summary>
		Task PersistAsync(CertificateType persistenceType, IPersistableCertificate certificate);
		
		/// <summary>
		/// Optional. The async method to use for fetching previously generated data for a given key.
		/// </summary>
		Task<IKeyCertificate> RetrieveAccountCertificateAsync();

		/// <summary>
		/// Optional. The async method to use for fetching previously generated data for a given key.
		/// </summary>
		Task<IAbstractCertificate> RetrieveSiteCertificateAsync();
	}
}
