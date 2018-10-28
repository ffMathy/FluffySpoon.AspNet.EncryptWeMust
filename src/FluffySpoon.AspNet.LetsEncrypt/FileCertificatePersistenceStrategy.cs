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

		public Task PersistAsync(string key, byte[] certificateBytes)
		{
			lock (typeof(FileCertificatePersistenceStrategy))
				File.WriteAllBytes(GetCertificatePath(key), certificateBytes);

			return Task.CompletedTask;
		}

		public Task<byte[]> RetrieveAsync(string key)
		{
			lock (typeof(FileCertificatePersistenceStrategy))
			{
				if (!File.Exists(GetCertificatePath(key)))
					return Task.FromResult<byte[]>(null);

				return Task.FromResult(File.ReadAllBytes(GetCertificatePath(key)));
			}
		}

		private string GetCertificatePath(string key)
		{
			return relativeFilePath + "_" + key;
		}
	}
}
