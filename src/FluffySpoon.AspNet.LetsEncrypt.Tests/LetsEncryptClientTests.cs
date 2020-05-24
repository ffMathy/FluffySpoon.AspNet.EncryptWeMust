using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Certes;
using Certes.Acme;
using FluentAssertions;
using FluentAssertions.Extensions;
using FluffySpoon.AspNet.LetsEncrypt.Certes;
using FluffySpoon.AspNet.LetsEncrypt.Certificates;
using FluffySpoon.AspNet.LetsEncrypt.Persistence;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using static FluffySpoon.AspNet.LetsEncrypt.Certificates.CertificateRenewalStatus;

namespace FluffySpoon.AspNet.LetsEncrypt.Tests
{
    [TestClass]
    public class LetsEncryptClientTests
    {
        private IPersistenceService PersistenceService;
        private ICertificateValidator CertificateValidator;
        private ILetsEncryptClientFactory LetsEncryptClientFactory;
        private ILetsEncryptClient LetsEncryptClient;
        
        private CertificateProvider Sut;
        
        [TestInitialize]
        public void Initialize()
        {
            var persistenceService = Substitute.For<IPersistenceService>();
            
            var options = new LetsEncryptOptions
            {
                Domains = new[] { "test.com" },
                Email = "test@test.com",
                KeyAlgorithm = KeyAlgorithm.ES512,
                UseStaging = true,
            };

            var certificateValidator = Substitute.For<ICertificateValidator>();
            
            certificateValidator.IsCertificateValid(null).Returns(false);
            certificateValidator.IsCertificateValid(RefEq(InvalidCert)).Returns(false);
            certificateValidator.IsCertificateValid(RefEq(ValidCert)).Returns(true);

            var client = Substitute.For<ILetsEncryptClient>();
            var factory = Substitute.For<ILetsEncryptClientFactory>();

            factory.GetClient().Returns(Task.FromResult(client));

            var sut = new CertificateProvider(
                options,
                certificateValidator,
                persistenceService,
                factory,
                NullLogger<CertificateProvider>.Instance);
           
            PersistenceService = persistenceService;
            CertificateValidator = certificateValidator;
            LetsEncryptClientFactory = factory;
            LetsEncryptClient = client;

            Sut = sut;
        }
        
        private static IAbstractCertificate ValidCert { get; } = SelfSignedCertificate.Make(
            DateTime.Now, 
            DateTime.Now.AddDays(90));
        
        private static IAbstractCertificate InvalidCert { get; } = SelfSignedCertificate.Make(
            DateTime.Now.Subtract(180.Days()),
            DateTime.Now.Subtract(90.Days()));

        [TestMethod]
        public async Task Should_TolerateNullInput()
        {
            PersistenceService.GetPersistedSiteCertificateAsync()
                .Returns(Task.FromResult(ValidCert));
            
            var output = await Sut.RenewCertificateIfNeeded(null);

            output.Status.Should().Be(LoadedFromStore);
            Assert.AreSame(ValidCert, output.Certificate);
        }

        [TestMethod]
        public async Task OnValidMemoryCertificate_ShouldNotAttemptRenewal()
        {
            var input = ValidCert;
            var output = await Sut.RenewCertificateIfNeeded(input);

            output.Status.Should().Be(Unchanged);
            ReferenceEquals(input, output.Certificate).Should().BeTrue();
        }

        [TestMethod]
        public async Task OnValidPersistedCertificate_ShouldNotAttemptRenewal()
        {
            var input = InvalidCert;
            var stored = ValidCert; 
            
            PersistenceService.GetPersistedSiteCertificateAsync().Returns(Task.FromResult(stored));
            
            var output = await Sut.RenewCertificateIfNeeded(input);

            output.Status.Should().Be(LoadedFromStore);
            Assert.AreSame(stored, output.Certificate);
        }

        [TestMethod]
        public async Task OnNoValidCertificateAvailable_ShouldRenewCertificate()
        {
            // arrange
            
            PersistenceService.GetPersistedSiteCertificateAsync().Returns(Task.FromResult(InvalidCert));

            var dtos = new []{ new ChallengeDto { Domains = new[] {"test.com"},  Token = "ping",  Response = "pong" } };
            var placedOrder = new PlacedOrder(dtos, Substitute.For<IOrderContext>(), Array.Empty<IChallengeContext>());

            LetsEncryptClient.PlaceOrder(SeqEq(new[] {"test.com"})).Returns(Task.FromResult(placedOrder));
            PersistenceService.PersistChallengesAsync(dtos).Returns(Task.CompletedTask);
            PersistenceService.DeleteChallengesAsync(dtos).Returns(Task.CompletedTask);

            var newCertBytes = SelfSignedCertificate.Make(DateTime.Now, DateTime.Now.AddDays(90)).RawData;
            
            LetsEncryptClient.FinalizeOrder(placedOrder).Returns(Task.FromResult(new PfxCertificate(newCertBytes)));

            var newCertificate = new LetsEncryptX509Certificate(newCertBytes) as IPersistableCertificate;
            PersistenceService.PersistSiteCertificateAsync(newCertificate).Returns(Task.CompletedTask);
            
            // act
            
            var output = await Sut.RenewCertificateIfNeeded(current: null);
            
            // assert
            
            output.Status.Should().Be(Renewed);
            ((LetsEncryptX509Certificate) output.Certificate).RawData.Should().BeEquivalentTo(newCertBytes);

            CertificateValidator.Received(1).IsCertificateValid(null);
            await PersistenceService.Received(1).GetPersistedSiteCertificateAsync();
            CertificateValidator.Received(1).IsCertificateValid(InvalidCert);
            await LetsEncryptClient.Received(1).PlaceOrder(SeqEq(new[] {"test.com"}));
            await PersistenceService.Received(1).PersistChallengesAsync(dtos);
            await PersistenceService.Received(1).DeleteChallengesAsync(dtos);
            await PersistenceService.Received(1).PersistSiteCertificateAsync(newCertificate);
            await PersistenceService.Received(1).PersistChallengesAsync(dtos);
            await LetsEncryptClient.Received(1).FinalizeOrder(placedOrder);
            await LetsEncryptClientFactory.Received(1).GetClient();
        }

        private static T[] SeqEq<T>(T[] xs) => Arg.Is<T[]>(ys => xs.SequenceEqual(ys)); 
        private static T RefEq<T>(T it) => Arg.Is<T>(x => ReferenceEquals(x, it));
    }
}