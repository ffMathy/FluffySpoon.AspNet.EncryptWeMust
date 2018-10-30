using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using FluffySpoon.AspNet.LetsEncrypt.Persistence;
using Microsoft.Extensions.Caching.Distributed;

namespace FluffySpoon.AspNet.LetsEncrypt.Redis
{
	public class DistributedCachePersistenceStrategy : CustomPersistenceStrategy
	{
		private const string KeyPrefix = "FluffySpoon_";

		public DistributedCachePersistenceStrategy(IDistributedCache cache, TimeSpan expiry) : base(
			async (key, bytes) => await cache.SetAsync(KeyPrefix + key, bytes, new DistributedCacheEntryOptions() {
				AbsoluteExpirationRelativeToNow = expiry
			}), 
			async (key) => await cache.GetAsync(KeyPrefix + key))
		{
		}
	}
}
