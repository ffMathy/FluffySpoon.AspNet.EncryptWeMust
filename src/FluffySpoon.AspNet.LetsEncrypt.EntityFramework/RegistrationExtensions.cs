using FluffySpoon.AspNet.LetsEncrypt.EntityFramework;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace FluffySpoon.AspNet.LetsEncrypt
{
	public static class RegistrationExtensions
	{
		public static void AddFluffySpoonLetsEncryptEntityFrameworkPersistence<TDbContext>(
			this IServiceCollection services,
			Func<TDbContext, string, byte[], Task> persistAsync,
			Func<TDbContext, string, Task<byte[]>> retrieveAsync)
		where TDbContext : DbContext
		{
			services.AddFluffySpoonLetsEncryptCertificatePersistence(
				(provider) => new EntityFrameworkCertificatePersistenceStrategy<TDbContext>(
					services.BuildServiceProvider(),
					persistAsync,
					retrieveAsync));
		}
	}
}
