using System;
using System.Collections.Generic;
using FluentAssertions;
using FluentAssertions.Extensions;
using FluffySpoon.AspNet.LetsEncrypt.Logic;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FluffySpoon.AspNet.LetsEncrypt.Tests
{
    [TestClass]
    public class CertificateValidatorTests
    {
        [TestMethod]
        public void OnNullCert_ShouldReturnFalse()
        {
            var certificateValidator = new CertificateValidator(
                new LetsEncryptOptions(),
                new NullLogger<CertificateValidator>());

            certificateValidator.IsCertificateValid(null).Should().BeFalse();
        } 
        
        [TestMethod]
        [DynamicData(nameof(ValidateCertificateDate), DynamicDataSourceType.Method)]
        public void ValidateCertificateTests(CertificateDates cd, ValidatorSettings vs, bool expected)
        {
            var certificateValidator = new CertificateValidator(
                new LetsEncryptOptions
                {
                    TimeUntilExpiryBeforeRenewal = vs.TimeUntilExpiryBeforeRenewal,
                    TimeAfterIssueDateBeforeRenewal = vs.TimeAfterIssueDateBeforeRenewal
                },
                new NullLogger<CertificateValidator>());

            var cert = SelfSignedCertificate.Make(cd.From, cd.To);

            certificateValidator.IsCertificateValid(cert).Should().Be(expected);
        }
        
        public struct CertificateDates
        {
            public CertificateDates(DateTime from, DateTime to)
            {
                From = from;
                To = to;
            }

            public DateTime From;
            public DateTime To;

            public override string ToString()
            {
                return $"CertificateDates: [{From:d}-{To:d}]";
            }
        }

        public struct ValidatorSettings
        {
            public ValidatorSettings(TimeSpan? timeUntilExpiryBeforeRenewal, TimeSpan? timeAfterIssueDateBeforeRenewal)
            {
                TimeUntilExpiryBeforeRenewal = timeUntilExpiryBeforeRenewal;
                TimeAfterIssueDateBeforeRenewal = timeAfterIssueDateBeforeRenewal;
            }
            
            public TimeSpan? TimeUntilExpiryBeforeRenewal;
            public TimeSpan? TimeAfterIssueDateBeforeRenewal;

            public override string ToString()
            {
                string Show(TimeSpan? ts) => ts == null ? "Never" : ts.Value.ToString("g");
                
                return $"ValidatorSettings: ({Show(TimeUntilExpiryBeforeRenewal)}, {Show(TimeAfterIssueDateBeforeRenewal)})";
            }
        }
        
        private static IEnumerable<object[]> ValidateCertificateDate()
        {
            // fresh certificate
            yield return Make(
                DateTime.Now.Subtract(1.Days()).Date, 
                DateTime.Now.AddDays(90).Date, 
                null, 
                TimeSpan.FromDays(30).As<TimeSpan?>(), 
                true 
            );
            
            // fresh certificate soon to expire
            yield return Make(
                DateTime.Now.Subtract(10.Days()).Date, 
                DateTime.Now.AddDays(10).Date, 
                TimeSpan.FromDays(30).As<TimeSpan?>(), 
                null, 
                false 
            );
            
            // close to expiry certificate mode 2
            yield return Make(
                DateTime.Now.Subtract(10.Days()).Date, 
                DateTime.Now.AddDays(10).Date, 
                null, 
                TimeSpan.FromDays(30), 
                true); 
            
            // future certificate
            yield return Make(
                DateTime.Now.AddDays(10).Date, 
                DateTime.Now.AddDays(20).Date, 
                null, 
                TimeSpan.FromDays(30), 
                false); 
            
            // past certificate
            yield return Make(
                DateTime.Now.Subtract(20.Days()).Date, 
                DateTime.Now.Subtract(10.Days()).Date, 
                null, 
                TimeSpan.FromDays(30), 
                false); 
            
            object[] Make(
                DateTime certStart,
                DateTime certEnd,
                TimeSpan? timeUntilExpiryBeforeRenewal,
                TimeSpan? timeAfterIssueDateBeforeRenewal,
                bool isValid)
            {
                return new object[]
                {
                    new CertificateDates(certStart,  certEnd),  
                    new ValidatorSettings(timeUntilExpiryBeforeRenewal,  timeAfterIssueDateBeforeRenewal),
                    isValid
                };
            }
        }
    }
}