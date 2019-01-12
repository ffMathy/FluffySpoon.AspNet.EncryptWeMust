using System;
using System.Threading.Tasks;
using FluffySpoon.AspNet.LetsEncrypt.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FluffySpoon.AspNet.LetsEncrypt.EntityFramework
{
	public class EntityFrameworkPersistenceStrategy<TDbContext> : CustomPersistenceStrategy
		where TDbContext : DbContext
	{
		public EntityFrameworkPersistenceStrategy(
			ServiceProvider serviceProvider,
			Func<TDbContext, PersistenceType, byte[], Task> persistAsync,
			Func<TDbContext, PersistenceType, Task<byte[]>> retrieveAsync) : base(
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
