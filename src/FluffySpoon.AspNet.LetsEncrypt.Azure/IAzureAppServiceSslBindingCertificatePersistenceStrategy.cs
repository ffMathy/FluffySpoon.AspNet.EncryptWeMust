using FluffySpoon.AspNet.LetsEncrypt.Persistence;
using System.Threading.Tasks;

namespace FluffySpoon.LetsEncrypt.Azure
{
	public interface IAzureAppServiceSslBindingCertificatePersistenceStrategy
	{
		Task PersistAsync(CertificateType persistenceType, byte[] bytes);
		Task<byte[]> RetrieveAsync(CertificateType persistenceType);
	}
}