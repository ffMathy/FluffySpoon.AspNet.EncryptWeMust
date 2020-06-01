using System;
using System.Threading.Tasks;
using Certes;
using FluffySpoon.AspNet.LetsEncrypt.Certificates;
using FluffySpoon.AspNet.LetsEncrypt.Persistence;
using FluffySpoon.AspNet.LetsEncrypt.Tests;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FluffySpoon.AspNet.LetsEncrypt.Azure
{
    [TestClass]
    public class PersistenceServiceTests
    {
        private IPersistenceService PersistenceService { get; set; }

        [TestInitialize]
        public void Initialize()
        {
            PersistenceService = new PersistenceService(
                new[] {new MemoryCertificatePersistenceStrategy()},
                new[] {new MemoryChallengePersistenceStrategy()},
                NullLogger<IPersistenceService>.Instance);
        }

        [TestMethod]
        public async Task MissingAccountCertificateReturnsNull()
        {
            Assert.IsNull(await PersistenceService.GetPersistedAccountCertificateAsync());
        }

        [TestMethod]
        public async Task MissingSiteCertificateReturnsNull()
        {
            Assert.IsNull(await PersistenceService.GetPersistedSiteCertificateAsync());
        }
       
        [TestMethod]
        public async Task AccountCertificateRoundTrip()
        {
            var key = KeyFactory.NewKey(KeyAlgorithm.ES256);

            await PersistenceService.PersistAccountCertificateAsync(key);

            var retrievedKey = await PersistenceService.GetPersistedAccountCertificateAsync();
            
            Assert.AreEqual(key.ToPem(), retrievedKey.ToPem());
        }

        [TestMethod]
        public async Task SiteCertificateRoundTrip()
        {
            var testCert = SelfSignedCertificate.Make(new DateTime(2020, 5, 24), new DateTime(2020, 5, 26));; 

            await PersistenceService.PersistSiteCertificateAsync(testCert);

            var retrievedCert = (LetsEncryptX509Certificate)await PersistenceService.GetPersistedSiteCertificateAsync();
            
            Assert.AreEqual(testCert.RawData, retrievedCert.RawData);
        }
    }
}