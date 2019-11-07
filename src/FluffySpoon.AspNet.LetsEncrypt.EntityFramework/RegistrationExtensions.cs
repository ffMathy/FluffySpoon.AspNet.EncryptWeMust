using FluffySpoon.AspNet.LetsEncrypt.EntityFramework;
using FluffySpoon.AspNet.LetsEncrypt.Persistence;
using FluffySpoon.AspNet.LetsEncrypt.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FluffySpoon.AspNet.LetsEncrypt
{
	public static class RegistrationExtensions
	{
		public static void AddFluffySpoonLetsEncryptEntityFrameworkCertificatePersistence<TDbContext>(
			this IServiceCollection services,
			Func<TDbContext, CertificateType, byte[], Task> persistAsync,
			Func<TDbContext, CertificateType, Task<byte[]>> retrieveAsync)
		where TDbContext : DbContext
		{
			services.AddFluffySpoonLetsEncryptCertificatePersistence(
				(provider) => new EntityFrameworkCertificatePersistenceStrategy<TDbContext>(
					services.BuildServiceProvider(),
					persistAsync,
					retrieveAsync));
		}

		public static void AddFluffySpoonLetsEncryptEntityFrameworkChallengePersistence<TDbContext>(
			this IServiceCollection services,
			Func<TDbContext, IEnumerable<ChallengeDto>, Task> persistAsync,
			Func<TDbContext, Task<IEnumerable<ChallengeDto>>> retrieveAsync,
			Func<TDbContext, IEnumerable<ChallengeDto>, Task> deleteAsync)
		where TDbContext : DbContext
		{
			services.AddFluffySpoonLetsEncryptChallengePersistence(
				(provider) => new EntityFrameworkChallengePersistenceStrategy<TDbContext>(
					services.BuildServiceProvider(),
					persistAsync,
					retrieveAsync,
					deleteAsync
				));
		}
	}
}
