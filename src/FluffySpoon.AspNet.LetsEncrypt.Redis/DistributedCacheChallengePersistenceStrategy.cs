using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluffySpoon.AspNet.LetsEncrypt.Persistence;
using FluffySpoon.AspNet.LetsEncrypt.Persistence.Models;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace FluffySpoon.AspNet.LetsEncrypt.Redis
{
	public class DistributedCacheChallengePersistenceStrategy : IChallengePersistenceStrategy
	{
		private const string Key = "FluffySpoon_Challenges";

		private readonly ILogger<DistributedCacheChallengePersistenceStrategy> _logger;
		private readonly IDistributedCache _cache;
		private readonly TimeSpan _expiry;

		public DistributedCacheChallengePersistenceStrategy(
			ILogger<DistributedCacheChallengePersistenceStrategy> logger,
			IDistributedCache cache, 
			TimeSpan expiry)
		{
			_logger = logger;
			_cache = cache;
			_expiry = expiry;
		}

		public bool CanHandleChallengeType(ChallengeType challengeType)
		{
			return challengeType == ChallengeType.Http01;
		}

		public async Task DeleteAsync(IEnumerable<ChallengeDto> challenges)
		{
			var persistedChallenges = await RetrieveAsync();
			var challengesToPersist = persistedChallenges
				.Where(x =>
					!challenges.Any(y => y.Token == x.Token))
				.ToList();

			await PersistAsync(challengesToPersist);
		}

		public async Task PersistAsync(IEnumerable<ChallengeDto> challenges)
		{
			var json = challenges == null ? null : JsonConvert.SerializeObject(challenges.ToArray());
			_logger.LogDebug("Persisting challenges {0}", json);

			var bytes = json == null ? null : Encoding.UTF8.GetBytes(json);

			await _cache.SetAsync(Key, bytes, new DistributedCacheEntryOptions()
			{
				AbsoluteExpirationRelativeToNow = _expiry
			});
		}

		public async Task<IEnumerable<ChallengeDto>> RetrieveAsync()
		{
			var bytes = await _cache.GetAsync(Key);
			var json = Encoding.UTF8.GetString(bytes);
			var challenges = JsonConvert.DeserializeObject<IEnumerable<ChallengeDto>>(json);

			return challenges;
		}
	}
}
