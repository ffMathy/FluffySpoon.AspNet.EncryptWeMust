using FluffySpoon.AspNet.LetsEncrypt.Persistence;
using System.Threading.Tasks;

namespace FluffySpoon.LetsEncrypt.Azure
{
	public interface IAzureAppServiceSslBindingCertificatePersistenceStrategy
	{
		Task PersistAsync(PersistenceType persistenceType, byte[] bytes);
		Task<byte[]> RetrieveAsync(PersistenceType persistenceType);
	}
}