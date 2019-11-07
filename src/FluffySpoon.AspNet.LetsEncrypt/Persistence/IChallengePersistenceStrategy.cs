using FluffySpoon.AspNet.LetsEncrypt.Persistence.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FluffySpoon.AspNet.LetsEncrypt.Persistence
{
	public interface IChallengePersistenceStrategy
	{
		/// <summary>
		/// Gets the challenge types that are supported by this strategy.
		/// </summary>
		/// <returns></returns>
		IEnumerable<ChallengeType> GetSupportedChallengeTypes();

		/// <summary>
		/// The async method to use for persisting a challenge.
		/// </summary>
		Task PersistAsync(IEnumerable<ChallengeDto> challenges);

		/// <summary>
		/// The async method to use for persisting a challenge.
		/// </summary>
		Task<IEnumerable<ChallengeDto>> RetrieveAsync();

		/// <summary>
		/// Optional. The async method to use for deleting a challenge after validation has completed.
		/// </summary>
		Task DeleteAsync(IEnumerable<ChallengeDto> challenges);
	}
}
