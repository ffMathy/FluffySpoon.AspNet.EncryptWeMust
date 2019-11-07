using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluffySpoon.AspNet.LetsEncrypt.Persistence;
using FluffySpoon.AspNet.LetsEncrypt.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FluffySpoon.AspNet.LetsEncrypt.EntityFramework
{
	public class EntityFrameworkChallengePersistenceStrategy<TDbContext> : CustomChallengePersistenceStrategy
		where TDbContext : DbContext
	{
		public EntityFrameworkChallengePersistenceStrategy(
			ServiceProvider serviceProvider,
			Func<TDbContext, IEnumerable<ChallengeDto>, Task> persistAsync,
			Func<TDbContext, Task<IEnumerable<ChallengeDto>>> retrieveAsync,
			Func<TDbContext, IEnumerable<ChallengeDto>, Task> deleteAsync) : base(
				new ChallengeType[] { ChallengeType.Http01 },
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
					using (var transaction = await databaseContext.Database.BeginTransactionAsync())
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
