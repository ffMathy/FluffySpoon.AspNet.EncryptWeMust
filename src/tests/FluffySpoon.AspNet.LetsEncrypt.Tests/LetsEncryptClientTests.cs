using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Certes;
using FluentAssertions;
using FluffySpoon.AspNet.LetsEncrypt.Logic;
using FluffySpoon.AspNet.LetsEncrypt.Persistence;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using static FluffySpoon.AspNet.LetsEncrypt.Logic.CertificateRenewalStatus;

namespace FluffySpoon.AspNet.LetsEncrypt.Tests
{
    public class LetsEncryptClientTests
    {
        private class SutWithMocks
        {
            public Mock<IPersistenceService> PersistenceService { get; set; }
            public Mock<ICertificateValidator> CertificateValidator { get; set; }
            public Mock<ILogger<ILetsEncryptClient>> Logger { get; set; }
            public LetsEncryptClient SUT { get; set; }
        }

        private static SutWithMocks GetSut()
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
            
            var logger = new Mock<ILogger<ILetsEncryptClient>>(MockBehavior.Loose);
            
            var sut = new LetsEncryptClient(
                options,
                persistenceService.Object,
                certificateValidator.Object,
                logger.Object);

            return new SutWithMocks
            {
                PersistenceService = persistenceService,
                CertificateValidator = certificateValidator,
                Logger = logger,
                SUT = sut
            };
        }
        
        private static X509Certificate2 ValidCert { get; } = new X509Certificate2();
        private static X509Certificate2 InvalidCert { get; } = new X509Certificate2();
        
        [Fact]
        public async Task OnValidMemoryCertificate_ShouldNotAttemptRenewal()
        {
            var sut = GetSut();
            
            var input = ValidCert;
            var output = await sut.SUT.AttemptCertificateRenewal(input);

            output.Status.Should().Be(Unchanged);
            ReferenceEquals(input, output.Certificate).Should().BeTrue();
        }
        
        [Fact]
        public async Task OnValidPersistedCertificate_ShouldNotAttemptRenewal()
        {
            var sut = GetSut();

            var input = InvalidCert;
            var stored = ValidCert; 
            
            sut.PersistenceService.Setup(x => x.GetPersistedSiteCertificateAsync()).ReturnsAsync(stored);
            
            var output = await sut.SUT.AttemptCertificateRenewal(input);

            output.Status.Should().Be(LoadedFromStore);
            Assert.Same(stored, output.Certificate);
        }
        
        [Fact]
        public async Task Should_TolerateNullInput()
        {
            var sut = GetSut();

            var stored = ValidCert; 
            
            sut.PersistenceService.Setup(x => x.GetPersistedSiteCertificateAsync()).ReturnsAsync(stored);
            
            var output = await sut.SUT.AttemptCertificateRenewal(null);

            output.Status.Should().Be(LoadedFromStore);
            Assert.Same(stored, output.Certificate);
        }


        private static T RefEq<T>(T it) => It.Is<T>(x => ReferenceEquals(x, it));
    }
}