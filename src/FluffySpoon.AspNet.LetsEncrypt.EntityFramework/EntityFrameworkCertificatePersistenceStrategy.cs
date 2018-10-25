using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace FluffySpoon.AspNet.LetsEncrypt.EntityFramework
{
	public class EntityFrameworkCertificatePersistenceStrategy<TDbContext> : CustomCertificatePersistenceStrategy
		where TDbContext : DbContext
	{
		public EntityFrameworkCertificatePersistenceStrategy(
			TDbContext databaseContext,
			Func<TDbContext, byte[], Task> persistAsync,
			Func<TDbContext, Task<byte[]>> retrieveAsync) : base(
				async (bytes) =>
				{
					using (var transaction = await databaseContext.Database.BeginTransactionAsync())
					{
						await persistAsync(databaseContext, bytes);
						await databaseContext.SaveChangesAsync();

						transaction.Commit();
					}
				},
				async () => {
					using (var transaction = await databaseContext.Database.BeginTransactionAsync())
					{
						return await retrieveAsync(databaseContext);
					}
				})
		{
		}
	}
}
