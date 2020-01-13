using System.Threading.Tasks;
using FluffySpoon.AspNet.LetsEncrypt.Persistence;

namespace FluffySpoon.AspNet.LetsEncrypt.Azure
{
	public interface IAzureAppServiceSslBindingCertificatePersistenceStrategy
	{
		Task PersistAsync(CertificateType persistenceType, byte[] bytes);
		Task<byte[]> RetrieveAsync(CertificateType persistenceType);
	}
}