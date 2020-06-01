using System;
using System.Threading.Tasks;
using FluffySpoon.AspNet.LetsEncrypt.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FluffySpoon.AspNet.LetsEncrypt.EntityFramework
{
	public class EntityFrameworkCertificatePersistenceStrategy<TDbContext> : CustomCertificatePersistenceStrategy
		where TDbContext : DbContext
	{
		public EntityFrameworkCertificatePersistenceStrategy(
			ServiceProvider serviceProvider,
			Func<TDbContext, CertificateType, byte[], Task> persistAsync,
			Func<TDbContext, CertificateType, Task<byte[]>> retrieveAsync) : base(
				async (key, bytes) =>
				{
					using (var scope = serviceProvider.CreateScope())
					using (var databaseContext = scope.ServiceProvider.GetRequiredService<TDbContext>())
					using (var transaction = await databaseContext.Database.BeginTransactionAsync())
					{
						await persistAsync(databaseContext, key, bytes);
						await databaseContext.SaveChangesAsync();

						transaction.Commit();
					}
				},
				async (key) =>
				{
					using (var scope = serviceProvider.CreateScope())
					using (var databaseContext = scope.ServiceProvider.GetRequiredService<TDbContext>())
					using (var transaction = await databaseContext.Database.BeginTransactionAsync())
					{
						return await retrieveAsync(databaseContext, key);
					}
				})
		{
		}
	}
}
