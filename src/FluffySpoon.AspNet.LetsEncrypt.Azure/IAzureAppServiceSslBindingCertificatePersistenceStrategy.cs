using System.Threading.Tasks;

namespace FluffySpoon.LetsEncrypt.Azure
{
	public interface IAzureAppServiceSslBindingCertificatePersistenceStrategy
	{
		Task PersistAsync(string key, byte[] bytes);
		Task<byte[]> RetrieveAsync(string key);
	}
}