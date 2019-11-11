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
	public class DistributedCacheChallengePersistenceStrategy : CustomChallengePersistenceStrategy
	{
		private const string Key = "FluffySpoon_Challenges";

		public DistributedCacheChallengePersistenceStrategy(
			ILogger<DistributedCacheChallengePersistenceStrategy> logger,
			IDistributedCache cache, 
			TimeSpan expiry) : base(
				new ChallengeType[] { ChallengeType.Http01 },
				async (challenges) =>
				{
					var json = challenges == null ? null : JsonConvert.SerializeObject(challenges.ToArray());
					logger.LogDebug("Persisting challenges {0}", json);

					var bytes = json == null ? null : Encoding.UTF8.GetBytes(json);

					await cache.SetAsync(Key, bytes, new DistributedCacheEntryOptions()
					{
						AbsoluteExpirationRelativeToNow = expiry
					});
				},
				async () => {
					var bytes = await cache.GetAsync(Key);
					var json = Encoding.UTF8.GetString(bytes);
					var challenges = JsonConvert.DeserializeObject<IEnumerable<ChallengeDto>>(json);

					return challenges;
				},
				async (challenges) =>
				{
					await cache.RemoveAsync(Key);
				})
		{
		}
	}
}
