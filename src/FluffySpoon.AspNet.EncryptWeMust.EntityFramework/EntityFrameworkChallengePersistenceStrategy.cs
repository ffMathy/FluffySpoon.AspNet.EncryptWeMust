using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluffySpoon.AspNet.EncryptWeMust.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FluffySpoon.AspNet.EncryptWeMust.EntityFramework
{
	public class EntityFrameworkChallengePersistenceStrategy<TDbContext> : CustomChallengePersistenceStrategy
		where TDbContext : DbContext
	{
		public EntityFrameworkChallengePersistenceStrategy(
			ServiceProvider serviceProvider,
			Func<TDbContext, IEnumerable<ChallengeDto>, Task> persistAsync,
			Func<TDbContext, Task<IEnumerable<ChallengeDto>>> retrieveAsync,
			Func<TDbContext, IEnumerable<ChallengeDto>, Task> deleteAsync) : base(
				async (challenges) =>
				{
					using (var scope = serviceProvider.CreateScope())
					using (var databaseContext = scope.ServiceProvider.GetRequiredService<TDbContext>())
					using (var transaction = await databaseContext.Database.BeginTransactionAsync())
					{
						await persistAsync(databaseContext, challenges);
						await databaseContext.SaveChangesAsync();

						transaction.Commit();
					}
				},
				async () =>
				{
					using (var scope = serviceProvider.CreateScope())
					using (var databaseContext = scope.ServiceProvider.GetRequiredService<TDbContext>())
					{
						return await retrieveAsync(databaseContext);
					}
				},
				async (challenges) =>
				{
					using (var scope = serviceProvider.CreateScope())
					using (var databaseContext = scope.ServiceProvider.GetRequiredService<TDbContext>())
					using (var transaction = await databaseContext.Database.BeginTransactionAsync())
					{
						await deleteAsync(databaseContext, challenges);
						await databaseContext.SaveChangesAsync();

						transaction.Commit();
					}
				})
		{
		}
	}
}
