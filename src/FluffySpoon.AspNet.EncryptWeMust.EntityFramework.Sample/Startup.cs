using System;
using System.Linq;
using Certes;
using FluffySpoon.AspNet.EncryptWeMust.Certes;
using FluffySpoon.AspNet.EncryptWeMust.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace FluffySpoon.AspNet.EncryptWeMust.EntityFramework.Sample
{
	public class Startup
	{
		// This method gets called by the runtime. Use this method to add services to the container.
		// For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
		public void ConfigureServices(IServiceCollection services)
		{
			services.AddEntityFrameworkInMemoryDatabase();
			services.AddDbContext<DatabaseContext>();

			services.AddFluffySpoonLetsEncryptEntityFrameworkCertificatePersistence<DatabaseContext>(
				async (databaseContext, key, bytes) => databaseContext.Certificates.Add(new Certificate() { 
					Bytes = bytes,
					Key = key.ToString()
				}),
				async (databaseContext, key) => databaseContext
					.Certificates
					.SingleOrDefault(x => x.Key == key.ToString())
					?.Bytes);

			services.AddFluffySpoonLetsEncryptEntityFrameworkChallengePersistence<DatabaseContext>(
				async (databaseContext, challenges) => databaseContext
					.Challenges
					.AddRange(
						challenges.Select(x =>
							new Challenge()
							{
								Token = x.Token,
								Response = x.Response,
								Domains = string.Join(",", x.Domains)
							})),
				async (databaseContext) => databaseContext
					.Challenges
					.Select(x =>
						new ChallengeDto()
						{
							Token = x.Token,
							Response = x.Response,
							Domains = x.Domains.Split(',', StringSplitOptions.RemoveEmptyEntries)
						}),
				async (databaseContext, challenges) => databaseContext
					.Challenges
					.RemoveRange(
						databaseContext
							.Challenges
							.Where(x => challenges.Any(y => y.Token == x.Token))
						));

			services.AddFluffySpoonLetsEncrypt(new LetsEncryptOptions()
			{
				Email = "some-email@github.com",
				UseStaging = true,
				Domains = new[] { Program.DomainToUse },
				TimeUntilExpiryBeforeRenewal = TimeSpan.FromDays(30),
				CertificateSigningRequest = new CsrInfo()
				{
					CountryName = "CountryNameStuff",
					Locality = "LocalityStuff",
					Organization = "OrganizationStuff",
					OrganizationUnit = "OrganizationUnitStuff",
					State = "StateStuff"
				}
			});

			services.AddFluffySpoonLetsEncryptFileCertificatePersistence();
			services.AddFluffySpoonLetsEncryptMemoryChallengePersistence();
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IHostingEnvironment env)
		{
			if (env.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
			}

			app.UseFluffySpoonLetsEncrypt();

			app.Run(async (context) =>
			{
				await context.Response.WriteAsync("Hello world");
			});
		}
	}
}
