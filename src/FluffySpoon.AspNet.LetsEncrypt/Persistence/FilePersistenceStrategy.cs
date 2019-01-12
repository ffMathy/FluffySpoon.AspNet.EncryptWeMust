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

		public Task PersistAsync(PersistenceType persistenceType, byte[] certificateBytes)
		{
			lock (typeof(FilePersistenceStrategy))
			{
				File.WriteAllBytes(
					GetCertificatePath(persistenceType),
					certificateBytes);
			}

			return Task.CompletedTask;
		}

		public Task<byte[]> RetrieveAsync(PersistenceType persistenceType)
		{
			lock (typeof(FilePersistenceStrategy))
			{
				if (!File.Exists(GetCertificatePath(persistenceType)))
					return Task.FromResult<byte[]>(null);

				return Task.FromResult(File.ReadAllBytes(GetCertificatePath(persistenceType)));
			}
		}

		private string GetCertificatePath(PersistenceType persistenceType)
		{
			return relativeFilePath + "_" + persistenceType.ToString();
		}
	}
}
