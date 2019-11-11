using System.IO;
using System.Threading.Tasks;

namespace FluffySpoon.AspNet.LetsEncrypt.Persistence
{
	public class FileCertificatePersistenceStrategy : ICertificatePersistenceStrategy
	{
		private readonly string relativeFilePath;

		public FileCertificatePersistenceStrategy(string relativeFilePath)
		{
			this.relativeFilePath = relativeFilePath;
		}

		public Task PersistAsync(CertificateType persistenceType, byte[] certificateBytes)
		{
			lock (typeof(FileCertificatePersistenceStrategy))
			{
				File.WriteAllBytes(
					GetCertificatePath(persistenceType),
					certificateBytes);
			}

			return Task.CompletedTask;
		}

		public Task<byte[]> RetrieveAsync(CertificateType persistenceType)
		{
			lock (typeof(FileCertificatePersistenceStrategy))
			{
				if (!File.Exists(GetCertificatePath(persistenceType)))
					return Task.FromResult<byte[]>(null);

				return Task.FromResult(File.ReadAllBytes(GetCertificatePath(persistenceType)));
			}
		}

		private string GetCertificatePath(CertificateType persistenceType)
		{
			return relativeFilePath + "_" + persistenceType.ToString();
		}
	}
}
