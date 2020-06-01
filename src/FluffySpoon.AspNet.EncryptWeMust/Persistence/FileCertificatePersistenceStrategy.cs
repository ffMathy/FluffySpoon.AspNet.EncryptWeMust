using System.IO;
using System.Threading.Tasks;
using FluffySpoon.AspNet.EncryptWeMust.Certificates;

namespace FluffySpoon.AspNet.EncryptWeMust.Persistence
{
	public class FileCertificatePersistenceStrategy : ICertificatePersistenceStrategy
	{
		private readonly string relativeFilePath;

		public FileCertificatePersistenceStrategy(string relativeFilePath)
		{
			this.relativeFilePath = relativeFilePath;
		}

        public Task PersistAsync(CertificateType persistenceType, IPersistableCertificate certificate)
		{
			lock (typeof(FileCertificatePersistenceStrategy))
			{
				File.WriteAllBytes(
					GetCertificatePath(persistenceType),
                    certificate.RawData);
			}

			return Task.CompletedTask;
		}

        public async Task<IKeyCertificate> RetrieveAccountCertificateAsync()
        {
	        var bytes = await ReadFile(CertificateType.Account);
	        if (bytes == null)
	        {
		        return null;
	        }
	        return new AccountKeyCertificate(bytes);
        }

        public async Task<IAbstractCertificate> RetrieveSiteCertificateAsync()
        {
	        var bytes = await ReadFile(CertificateType.Site);
	        if (bytes == null)
	        {
		        return null;
	        }
	        return new LetsEncryptX509Certificate(bytes);
        }

        private async Task<byte[]> ReadFile(CertificateType persistenceType)
		{
			lock (typeof(FileCertificatePersistenceStrategy))
			{
				if (!File.Exists(GetCertificatePath(persistenceType)))
                    return null;

                return File.ReadAllBytes(GetCertificatePath(persistenceType));
			}
		}

		private string GetCertificatePath(CertificateType persistenceType)
		{
			return relativeFilePath + "_" + persistenceType.ToString();
		}
	}
}