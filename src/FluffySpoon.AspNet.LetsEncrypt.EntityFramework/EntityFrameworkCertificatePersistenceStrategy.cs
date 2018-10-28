using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FluffySpoon.AspNet.LetsEncrypt.EntityFramework
{
	public class EntityFrameworkCertificatePersistenceStrategy<TDbContext> : CustomCertificatePersistenceStrategy
		where TDbContext : DbContext
	{
		public EntityFrameworkCertificatePersistenceStrategy(
			ServiceProvider serviceProvider,
			Func<TDbContext, byte[], Task> persistAsync,
			Func<TDbContext, Task<byte[]>> retrieveAsync) : base(
				async (bytes) =>
				{
					using (var scope = serviceProvider.CreateScope())
					using (var databaseContext = scope.ServiceProvider.GetRequiredService<TDbContext>())
					using (var transaction = await databaseContext.Database.BeginTransactionAsync())
					{
						await persistAsync(databaseContext, bytes);
						await databaseContext.SaveChangesAsync();

						transaction.Commit();
					}
				},
				async () =>
				{
					using (var scope = serviceProvider.CreateScope())
					using (var databaseContext = scope.ServiceProvider.GetRequiredService<TDbContext>())
					using (var transaction = await databaseContext.Database.BeginTransactionAsync())
					{
						return await retrieveAsync(databaseContext);
					}
				})
		{
		}
	}
}
