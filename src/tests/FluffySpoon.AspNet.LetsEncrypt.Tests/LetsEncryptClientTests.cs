using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Certes;
using Certes.Acme;
using FluentAssertions;
using FluentAssertions.Extensions;
using FluffySpoon.AspNet.LetsEncrypt.Logic;
using FluffySpoon.AspNet.LetsEncrypt.Logic.Models;
using FluffySpoon.AspNet.LetsEncrypt.Persistence;
using FluffySpoon.AspNet.LetsEncrypt.Persistence.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using static FluffySpoon.AspNet.LetsEncrypt.Logic.Models.CertificateRenewalStatus;

namespace FluffySpoon.AspNet.LetsEncrypt.Tests
{
    public class LetsEncryptClientTests
    {
        private Mock<IPersistenceService> PersistenceService { get; }
        private Mock<ICertificateValidator> CertificateValidator { get; }
        private Mock<ILetsEncryptClientFactory> LetsEncryptClientFactory { get; }
        private Mock<ILetsEncryptClient> LetsEncryptClient { get; }
        private CertificateProvider Sut { get; }
        
        public LetsEncryptClientTests()
        {
            var persistenceService = new Mock<IPersistenceService>(MockBehavior.Strict);
            
            var options = new LetsEncryptOptions
            {
                Domains = new[] { "test.com" },
                Email = "test@test.com",
                KeyAlgorithm = KeyAlgorithm.ES512,
                UseStaging = true,
            };

            var certificateValidator = new Mock<ICertificateValidator>(MockBehavior.Strict);
            
            certificateValidator.Setup(it => it.IsCertificateValid(null)).Returns(false);
            certificateValidator.Setup(it => it.IsCertificateValid(RefEq(InvalidCert))).Returns(false);
            certificateValidator.Setup(it => it.IsCertificateValid(RefEq(ValidCert))).Returns(true);
            
            var client = new Mock<ILetsEncryptClient>(MockBehavior.Strict);
            var factory = new Mock<ILetsEncryptClientFactory>(MockBehavior.Strict);
            factory.Setup(it => it.GetClient()).ReturnsAsync(client.Object);

            var sut = new CertificateProvider(
                options,
                certificateValidator.Object,
                persistenceService.Object,
                factory.Object,
                NullLogger<CertificateProvider>.Instance);
           
            PersistenceService = persistenceService;
            CertificateValidator = certificateValidator;
            LetsEncryptClientFactory = factory;
            LetsEncryptClient = client;

            Sut = sut;
        }
        
        private static X509Certificate2 ValidCert { get; } = SelfSignedCertificate.Make(
            DateTime.Now, 
            DateTime.Now.AddDays(90));
        
        private static X509Certificate2 InvalidCert { get; } = SelfSignedCertificate.Make(
            DateTime.Now.Subtract(180.Days()),
            DateTime.Now.Subtract(90.Days()));

        [Fact]
        public async Task Should_TolerateNullInput()
        {
            PersistenceService
                .Setup(x => x.GetPersistedSiteCertificateAsync())
                .ReturnsAsync(ValidCert);
            
            var output = await Sut.RenewCertificateIfNeeded(null);

            output.Status.Should().Be(LoadedFromStore);
            Assert.Same(ValidCert, output.Certificate);
        }

        [Fact]
        public async Task OnValidMemoryCertificate_ShouldNotAttemptRenewal()
        {
            var input = ValidCert;
            var output = await Sut.RenewCertificateIfNeeded(input);

            output.Status.Should().Be(Unchanged);
            ReferenceEquals(input, output.Certificate).Should().BeTrue();
        }

        [Fact]
        public async Task OnValidPersistedCertificate_ShouldNotAttemptRenewal()
        {
            var input = InvalidCert;
            var stored = ValidCert; 
            
            PersistenceService.Setup(x => x.GetPersistedSiteCertificateAsync()).ReturnsAsync(stored);
            
            var output = await Sut.RenewCertificateIfNeeded(input);

            output.Status.Should().Be(LoadedFromStore);
            Assert.Same(stored, output.Certificate);
        }

        [Fact]
        public async Task OnNoValidCertificateAvailable_ShouldRenewCertificate()
        {
            PersistenceService
                .Setup(x => x.GetPersistedSiteCertificateAsync())
                .ReturnsAsync(InvalidCert);

            var dtos = new []{ new ChallengeDto { Domains = new[] {"test.com"},  Token = "ping",  Response = "pong" } };
            var placedOrder = new PlacedOrder(dtos, new Mock<IOrderContext>().Object, Array.Empty<IChallengeContext>());
            
            LetsEncryptClient
                .Setup(x => x.PlaceOrder(new[] {"test.com"}))
                .ReturnsAsync(placedOrder);

            PersistenceService
                .Setup(x => x.PersistChallengesAsync(dtos))
                .Returns(Task.CompletedTask);
            
            PersistenceService
                .Setup(x => x.DeleteChallengesAsync(dtos))
                .Returns(Task.CompletedTask);

            var newCertBytes = SelfSignedCertificate
                .Make(DateTime.Now, DateTime.Now.AddDays(90))
                .RawData;
            
            LetsEncryptClient
                .Setup(x => x.FinalizeOrder(placedOrder))
                .ReturnsAsync(new PfxCertificate(newCertBytes));

            PersistenceService
                .Setup(x => x.PersistSiteCertificateAsync(newCertBytes))
                .Returns(Task.CompletedTask);
            
            var output = await Sut.RenewCertificateIfNeeded(current: null);
            
            output.Status.Should().Be(Renewed);
            output.Certificate.RawData.Should().BeEquivalentTo(newCertBytes);
            
            PersistenceService.VerifyAll();
            LetsEncryptClient.VerifyAll();
            LetsEncryptClientFactory.VerifyAll();
            CertificateValidator.Verify(x => x.IsCertificateValid(null));
            CertificateValidator.Verify(x => x.IsCertificateValid(InvalidCert));
        }
        
        private static T RefEq<T>(T it) => It.Is<T>(x => ReferenceEquals(x, it));
    }
}