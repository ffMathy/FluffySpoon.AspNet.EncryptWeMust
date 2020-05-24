using System;
using System.IO;
using System.Threading.Tasks;
using Certes;
using FluentAssertions;
using FluffySpoon.AspNet.LetsEncrypt.Certificates;
using FluffySpoon.AspNet.LetsEncrypt.Persistence;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FluffySpoon.AspNet.LetsEncrypt.Tests
{
    [TestClass]
    public class FileCertificatePersistence
    {
        string _testFolder;
        private ICertificatePersistenceStrategy Strategy { get; set; }

        [TestInitialize]
        public void Initialize()
        {
            _testFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Strategy = new FileCertificatePersistenceStrategy(_testFolder);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            if (_testFolder != null)
            {
                try
                {
                    Directory.Delete(_testFolder, true);
                }
                catch
                {
                }
            }
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

            testCert.RawData.Should().Equal(retrievedCert.RawData);
        }

        [TestMethod]
        public async Task SiteCertificateRoundTrip()
        {
            var testCert = SelfSignedCertificate.Make(new DateTime(2020, 5, 24), new DateTime(2020, 5, 26));; 

            await Strategy.PersistAsync(CertificateType.Site, testCert);

            var retrievedCert = (LetsEncryptX509Certificate)await Strategy.RetrieveSiteCertificateAsync();

            testCert.RawData.Should().Equal(retrievedCert.RawData);
        }
    }
}