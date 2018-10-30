using FluffySpoon.AspNet.LetsEncrypt.Redis;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace FluffySpoon.AspNet.LetsEncrypt
{
	public static class RegistrationExtensions
	{
		public static void AddFluffySpoonLetsEncryptDistributedCacheCertificatePersistence(
			this IServiceCollection services,
			string cacheKey)
		{
			services.AddFluffySpoonLetsEncryptCertificatePersistence(
				(provider) => new DistributedCachePersistenceStrategy(
					provider.GetRequiredService<IDistributedCache>()));
		}

		public static void AddFluffySpoonLetsEncryptDistributedCacheChallengePersistence(
			this IServiceCollection services,
			string cacheKey)
		{
			services.AddFluffySpoonLetsEncryptChallengePersistence(
				(provider) => new DistributedCachePersistenceStrategy(
					provider.GetRequiredService<IDistributedCache>()));
		}
	}
}
