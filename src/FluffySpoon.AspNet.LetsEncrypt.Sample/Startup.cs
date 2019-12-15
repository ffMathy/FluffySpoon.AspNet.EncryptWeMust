using System;
using Certes;
using FluffySpoon.AspNet.LetsEncrypt.Certes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace FluffySpoon.AspNet.LetsEncrypt.Sample
{
	public class Startup
	{
		// This method gets called by the runtime. Use this method to add services to the container.
		// For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
		public void ConfigureServices(IServiceCollection services)
		{
			services.AddFluffySpoonLetsEncryptRenewalService(new LetsEncryptOptions()
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
			services.AddFluffySpoonLetsEncryptFileChallengePersistence();
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IHostingEnvironment env)
		{
			if (env.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
			}

			app.UseFluffySpoonLetsEncryptChallengeApprovalMiddleware();

			app.Run(async (context) =>
			{
				await context.Response.WriteAsync("Hello world");
			});
		}
	}
}
