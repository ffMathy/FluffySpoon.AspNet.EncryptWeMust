using FluffySpoon.AspNet.LetsEncrypt.EntityFramework;
using FluffySpoon.AspNet.LetsEncrypt.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace FluffySpoon.AspNet.LetsEncrypt
{
	public static class RegistrationExtensions
	{
		public static void AddFluffySpoonLetsEncryptEntityFrameworkCertificatePersistence<TDbContext>(
			this IServiceCollection services,
			Func<TDbContext, PersistenceType, byte[], Task> persistAsync,
			Func<TDbContext, PersistenceType, Task<byte[]>> retrieveAsync)
		where TDbContext : DbContext
		{
			services.AddFluffySpoonLetsEncryptCertificatePersistence(
				(provider) => new EntityFrameworkPersistenceStrategy<TDbContext>(
					services.BuildServiceProvider(),
					persistAsync,
					retrieveAsync));
		}

		public static void AddFluffySpoonLetsEncryptEntityFrameworkChallengePersistence<TDbContext>(
			this IServiceCollection services,
			Func<TDbContext, PersistenceType, byte[], Task> persistAsync,
			Func<TDbContext, PersistenceType, Task<byte[]>> retrieveAsync)
		where TDbContext : DbContext
		{
			services.AddFluffySpoonLetsEncryptChallengePersistence(
				(provider) => new EntityFrameworkPersistenceStrategy<TDbContext>(
					services.BuildServiceProvider(),
					persistAsync,
					retrieveAsync));
		}
	}
}
