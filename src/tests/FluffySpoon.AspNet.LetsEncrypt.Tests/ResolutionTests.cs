using System;
using Certes;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FluffySpoon.AspNet.LetsEncrypt.Tests
{
    [TestClass]
    public class ResolutionTests
    {
        [TestMethod]
        public void Go()
        {
            var thing = WebHost.CreateDefaultBuilder()
                .ConfigureLogging(options => options.AddConsole())
                .ConfigureServices(services =>
                {
                    services.AddFluffySpoonLetsEncryptRenewalService(new LetsEncryptOptions()
                    {
                        Email = "some-email@github.com",
                        UseStaging = true,
                        Domains = new[] {"test.com"},
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
                })
                .Configure(appBuilder => { appBuilder.UseFluffySpoonLetsEncryptChallengeApprovalMiddleware(); })
                .Build();

            thing.Services.GetRequiredService<ILetsEncryptRenewalService>();
        }
    }
}