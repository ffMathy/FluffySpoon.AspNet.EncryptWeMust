using FluffySpoon.AspNet.LetsEncrypt.Persistence.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FluffySpoon.AspNet.LetsEncrypt.Persistence
{
	class MemoryChallengePersistenceStrategy : IChallengePersistenceStrategy
	{
		private IEnumerable<ChallengeDto> _challenges;

		public MemoryChallengePersistenceStrategy()
		{
			_challenges = new List<ChallengeDto>();
		}

		public Task DeleteAsync(IEnumerable<ChallengeDto> challenges)
		{
			_challenges = new List<ChallengeDto>();

			return Task.CompletedTask;
		}

		public bool CanHandleChallengeType(ChallengeType challengeType)
		{
			return challengeType == ChallengeType.Http01;
		}

		public Task PersistAsync(IEnumerable<ChallengeDto> challenges)
		{
			_challenges = challenges;

			return Task.CompletedTask;
		}

		public Task<IEnumerable<ChallengeDto>> RetrieveAsync()
		{
			return Task.FromResult(_challenges);
		}
	}
}
