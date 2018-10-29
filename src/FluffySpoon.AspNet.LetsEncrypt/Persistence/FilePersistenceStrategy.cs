using System.IO;
using System.Threading.Tasks;

namespace FluffySpoon.AspNet.LetsEncrypt.Persistence
{
	public class FilePersistenceStrategy : ICertificatePersistenceStrategy, IChallengePersistenceStrategy
	{
		private readonly string relativeFilePath;

		public FilePersistenceStrategy(string relativeFilePath)
		{
			this.relativeFilePath = relativeFilePath;
		}

		public Task PersistAsync(string key, byte[] certificateBytes)
		{
			lock (typeof(FilePersistenceStrategy))
				File.WriteAllBytes(GetCertificatePath(key), certificateBytes);

			return Task.CompletedTask;
		}

		public Task<byte[]> RetrieveAsync(string key)
		{
			lock (typeof(FilePersistenceStrategy))
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
