using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace FluffySpoon.AspNet.EncryptWeMust.EntityFramework.Sample
{
	public class DatabaseContext : DbContext
	{
		public DbSet<Certificate> Certificates { get; set; }
		public DbSet<Challenge> Challenges { get; set; }

		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		{
			base.OnConfiguring(optionsBuilder);

			optionsBuilder.UseInMemoryDatabase("FluffySpoon");
			optionsBuilder.ConfigureWarnings(x => {
				x.Ignore(InMemoryEventId.TransactionIgnoredWarning);
			});
		}
	}
}
