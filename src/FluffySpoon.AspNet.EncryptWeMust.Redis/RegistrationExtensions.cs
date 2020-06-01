using System;
using FluffySpoon.AspNet.LetsEncrypt.DistributedCache;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// ReSharper disable once CheckNamespace
namespace FluffySpoon.AspNet.LetsEncrypt
{
	public static class RegistrationExtensions
	{
		public static void AddFluffySpoonLetsEncryptDistributedCacheCertificatePersistence(
			this IServiceCollection services,
			TimeSpan expiry)
		{
			services.AddFluffySpoonLetsEncryptCertificatePersistence(
				(provider) => new DistributedCacheCertificatePersistenceStrategy(
					provider.GetRequiredService<ILogger<DistributedCacheCertificatePersistenceStrategy>>(),
					provider.GetRequiredService<IDistributedCache>(), 
					expiry));
		}

		public static void AddFluffySpoonLetsEncryptDistributedCacheChallengePersistence(
			this IServiceCollection services,
			TimeSpan expiry)
		{
			services.AddFluffySpoonLetsEncryptChallengePersistence(
				(provider) => new DistributedCacheChallengePersistenceStrategy(
					provider.GetRequiredService<ILogger<DistributedCacheChallengePersistenceStrategy>>(),
					provider.GetRequiredService<IDistributedCache>(), 
					expiry));
		}
	}
}
