using System;
using System.Threading.Tasks;
using Certes;
using FluffySpoon.AspNet.EncryptWeMust.Certificates;
using FluffySpoon.AspNet.EncryptWeMust.Persistence;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FluffySpoon.AspNet.EncryptWeMust.Tests
{
    [TestClass]
    public class CustomCertificatePersistence
    {
        private ICertificatePersistenceStrategy Strategy { get; set; }

        [TestInitialize]
        public void Initialize()
        {
            byte[] store = null;
            Strategy = new CustomCertificatePersistenceStrategy(
                (type, data) =>
                {
                    store = data;
                    return Task.CompletedTask;
                },
                (type) => Task.FromResult(store));
        }
        
        [TestMethod]
        public async Task MissingAccountCertificateReturnsNull()
        {
            var retrievedCert = (AccountKeyCertificate)await Strategy.RetrieveAccountCertificateAsync();
            Assert.IsNull(retrievedCert);
        }

        [TestMethod]
        public async Task MissingSiteCertificateReturnsNull()
        {
            var retrievedCert = (LetsEncryptX509Certificate)await Strategy.RetrieveSiteCertificateAsync();
            Assert.IsNull(retrievedCert);
        }

        [TestMethod]
        public async Task AccountCertificateRoundTrip()
        {
            var testCert = new AccountKeyCertificate(KeyFactory.NewKey(KeyAlgorithm.ES256));KeyFactory.NewKey(KeyAlgorithm.ES256); 

            await Strategy.PersistAsync(CertificateType.Account, testCert);

            var retrievedCert = (AccountKeyCertificate)await Strategy.RetrieveAccountCertificateAsync();

            Assert.AreEqual(testCert.RawData, retrievedCert.RawData);
        }

        [TestMethod]
        public async Task SiteCertificateRoundTrip()
        {
            var testCert = SelfSignedCertificate.Make(new DateTime(2020, 5, 24), new DateTime(2020, 5, 26));; 

            await Strategy.PersistAsync(CertificateType.Site, testCert);

            var retrievedCert = (LetsEncryptX509Certificate)await Strategy.RetrieveSiteCertificateAsync();

            Assert.AreEqual(testCert.RawData, retrievedCert.RawData);
        }
    }
}