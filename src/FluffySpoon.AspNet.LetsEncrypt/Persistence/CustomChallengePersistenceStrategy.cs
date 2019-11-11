using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluffySpoon.AspNet.LetsEncrypt.Persistence.Models;

namespace FluffySpoon.AspNet.LetsEncrypt.Persistence
{
	public delegate Task PersistChallengesDelegate(IEnumerable<ChallengeDto> challenges);
	public delegate Task DeleteChallengesDelegate(IEnumerable<ChallengeDto> challenges);
	public delegate Task<IEnumerable<ChallengeDto>> RetrieveChallengesDelegate();

	public class CustomChallengePersistenceStrategy : IChallengePersistenceStrategy
	{
		private readonly IEnumerable<ChallengeType> _supportedChallengeTypes;
		private readonly PersistChallengesDelegate _persistAsync;
		private readonly DeleteChallengesDelegate _deleteAsync;
		private readonly RetrieveChallengesDelegate _retrieveAsync;

		public CustomChallengePersistenceStrategy(
			IEnumerable<ChallengeType> supportedChallengeTypes,
			PersistChallengesDelegate persistAsync,
			RetrieveChallengesDelegate retrieveAsync,
			DeleteChallengesDelegate deleteAsync)
		{
			this._supportedChallengeTypes = supportedChallengeTypes;
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

		public bool CanHandleChallengeType(ChallengeType challengeType)
		{
			return _supportedChallengeTypes.Contains(challengeType);
		}
	}
}
