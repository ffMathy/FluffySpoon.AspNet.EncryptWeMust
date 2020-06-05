using System.Collections.Generic;
using System.Threading.Tasks;

namespace FluffySpoon.AspNet.EncryptWeMust.Persistence
{
	public delegate Task PersistChallengesDelegate(IEnumerable<ChallengeDto> challenges);
	public delegate Task DeleteChallengesDelegate(IEnumerable<ChallengeDto> challenges);
	public delegate Task<IEnumerable<ChallengeDto>> RetrieveChallengesDelegate();

	public class CustomChallengePersistenceStrategy : IChallengePersistenceStrategy
	{
		private readonly PersistChallengesDelegate _persistAsync;
		private readonly DeleteChallengesDelegate _deleteAsync;
		private readonly RetrieveChallengesDelegate _retrieveAsync;

		public CustomChallengePersistenceStrategy(
			PersistChallengesDelegate persistAsync,
			RetrieveChallengesDelegate retrieveAsync,
			DeleteChallengesDelegate deleteAsync)
		{
			this._persistAsync = persistAsync;
			this._deleteAsync = deleteAsync;
			this._retrieveAsync = retrieveAsync;
		}

		public Task PersistAsync(IEnumerable<ChallengeDto> challenges)
		{
			return _persistAsync(challenges);
		}

		public Task<IEnumerable<ChallengeDto>> RetrieveAsync()
		{
			return _retrieveAsync();
		}

		public Task DeleteAsync(IEnumerable<ChallengeDto> challenges)
		{
			return _deleteAsync(challenges);
		}
	}
}
