using FluffySpoon.AspNet.LetsEncrypt.Redis;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace FluffySpoon.AspNet.LetsEncrypt
{
	public static class RegistrationExtensions
	{
		public static void AddFluffySpoonLetsEncryptRedisCertificatePersistence(
			this IServiceCollection services,
			string cacheKey)
		{
			services.AddFluffySpoonLetsEncryptCertificatePersistence(
				(provider) => new RedisPersistenceStrategy(
					provider.GetRequiredService<IDistributedCache>()));
		}

		public static void AddFluffySpoonLetsEncryptRedisChallengePersistence(
			this IServiceCollection services,
			string cacheKey)
		{
			services.AddFluffySpoonLetsEncryptChallengePersistence(
				(provider) => new RedisPersistenceStrategy(
					provider.GetRequiredService<IDistributedCache>()));
		}
	}
}
