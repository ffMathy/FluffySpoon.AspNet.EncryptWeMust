using System.IO;
using System.Threading.Tasks;

namespace FluffySpoon.AspNet.LetsEncrypt
{
	public class FileCertificatePersistenceStrategy : ICertificatePersistenceStrategy
	{
		private readonly string relativeFilePath;

		public FileCertificatePersistenceStrategy(string relativeFilePath)
		{
			this.relativeFilePath = relativeFilePath;
		}

		public Task PersistAsync(byte[] certificateBytes)
		{
			lock (typeof(FileCertificatePersistenceStrategy))
				File.WriteAllBytes(relativeFilePath, certificateBytes);

			return Task.CompletedTask;
		}

		public Task<byte[]> RetrieveAsync()
		{
			lock (typeof(FileCertificatePersistenceStrategy))
			{
				if (!File.Exists(relativeFilePath))
					return Task.FromResult<byte[]>(null);

				return Task.FromResult(File.ReadAllBytes(relativeFilePath));
			}
		}
	}
}
