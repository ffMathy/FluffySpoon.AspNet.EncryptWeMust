using System.IO;
using System.Threading.Tasks;
using FluffySpoon.AspNet.LetsEncrypt.Certificates;

namespace FluffySpoon.AspNet.LetsEncrypt.Persistence
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
            return new AccountKeyCertificate(await ReadFile(CertificateType.Account));
        }

        public async Task<IAbstractCertificate> RetrieveSiteCertificateAsync()
        {
            return new LetsEncryptX509Certificate(await ReadFile(CertificateType.Site));
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