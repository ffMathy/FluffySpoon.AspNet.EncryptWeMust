using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluffySpoon.AspNet.LetsEncrypt.Persistence.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace FluffySpoon.AspNet.LetsEncrypt.Persistence
{
	public class FileChallengePersistenceStrategy : IChallengePersistenceStrategy
	{
		private readonly string _relativeFilePath;

		public FileChallengePersistenceStrategy(string relativeFilePath)
		{
			_relativeFilePath = relativeFilePath;
		}

		public Task DeleteAsync(IEnumerable<ChallengeDto> challenges)
		{
			throw new System.NotImplementedException();
		}

		public IEnumerable<ChallengeType> GetSupportedChallengeTypes()
		{
			return new ChallengeType[] { ChallengeType.Http01 };
		}

		public Task PersistAsync(IEnumerable<ChallengeDto> challenges)
		{
			var json = challenges == null ? null : JsonConvert.SerializeObject(challenges.ToArray());

			var bytes = json == null ? null : Encoding.UTF8.GetBytes(json);

			lock (typeof(FileChallengePersistenceStrategy))
			{
				File.WriteAllBytes(
					GetCertificatePath(),
					bytes);
			}

			return Task.CompletedTask;
		}

		public Task<IEnumerable<ChallengeDto>> RetrieveAsync()
		{
			lock (typeof(FileChallengePersistenceStrategy))
			{
				if (!File.Exists(GetCertificatePath()))
					return Task.FromResult<IEnumerable<ChallengeDto>>(new List<ChallengeDto>());

				var bytes = File.ReadAllBytes(GetCertificatePath());
				var json = Encoding.UTF8.GetString(bytes);
				var challenges = JsonConvert.DeserializeObject<IEnumerable<ChallengeDto>>(json);

				return Task.FromResult(challenges);
			}
		}

		private string GetCertificatePath()
		{
			return _relativeFilePath + "_Challenges";
		}
	}
}
