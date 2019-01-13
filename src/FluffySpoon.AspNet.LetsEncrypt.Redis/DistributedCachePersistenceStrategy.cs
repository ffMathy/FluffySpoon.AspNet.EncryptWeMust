using System;
using System.Threading.Tasks;
using FluffySpoon.AspNet.LetsEncrypt.Persistence;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace FluffySpoon.AspNet.LetsEncrypt.Redis
{
	public class DistributedCachePersistenceStrategy : CustomPersistenceStrategy
	{
		private const string KeyPrefix = "FluffySpoon_";

		public DistributedCachePersistenceStrategy(
			ILogger logger,
			IDistributedCache cache, 
			TimeSpan expiry) : base(
				async (key, bytes) =>
				{
					logger.LogInformation("Persisting {0} to distributed cache.", key);
					await cache.SetAsync(KeyPrefix + key, bytes, new DistributedCacheEntryOptions()
					{
						AbsoluteExpirationRelativeToNow = expiry
					});
				},
				async (key) => await cache.GetAsync(KeyPrefix + key))
		{
		}
	}
}
