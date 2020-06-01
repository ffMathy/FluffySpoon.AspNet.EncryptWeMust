using System;
using FluffySpoon.AspNet.EncryptWeMust.Persistence;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace FluffySpoon.AspNet.EncryptWeMust.DistributedCache
{
	public class DistributedCacheCertificatePersistenceStrategy : CustomCertificatePersistenceStrategy
	{
		private const string KeyPrefix = "FluffySpoon_";

		public DistributedCacheCertificatePersistenceStrategy(
			ILogger<DistributedCacheCertificatePersistenceStrategy> logger,
			IDistributedCache cache, 
			TimeSpan expiry) : base(
				async (key, bytes) =>
				{
					await cache.SetAsync(KeyPrefix + key, bytes, new DistributedCacheEntryOptions()
					{
						AbsoluteExpirationRelativeToNow = expiry
					});
				},
				async (key) => {
					return await cache.GetAsync(KeyPrefix + key);
				})
		{
		}
	}
}
