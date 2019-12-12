using System;
using System.Collections.Generic;
using FluentAssertions;
using FluentAssertions.Extensions;
using FluffySpoon.AspNet.LetsEncrypt.Logic;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace FluffySpoon.AspNet.LetsEncrypt.Tests
{
    public class CertificateValidatorTests
    {
        [Fact]
        public void OnNullCert_ShouldReturnFalse()
        {
            var certificateValidator = new CertificateValidator(
                new LetsEncryptOptions(),
                new NullLogger<CertificateValidator>());

            certificateValidator.IsCertificateValid(null).Should().BeFalse();
        } 
        
        [Theory]
        [MemberData(nameof(ValidateCertificateDate))]
        public void ValidateCertificateTests(DateTime from, DateTime to, TimeSpan? a, TimeSpan? b, bool expected)
        {
            var certificateValidator = new CertificateValidator(
                new LetsEncryptOptions
                {
                    TimeUntilExpiryBeforeRenewal = a,
                    TimeAfterIssueDateBeforeRenewal = b
                },
                new NullLogger<CertificateValidator>());

            var cert = SelfSignedCertificate.Make(from, to);

            certificateValidator.IsCertificateValid(cert).Should().Be(expected);
        }

        public static IEnumerable<object[]> ValidateCertificateDate()
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
            
            object[] Make(DateTime certStart, DateTime certEnd, TimeSpan? timeUntilExpiryBeforeRenewal, TimeSpan? timeAfterIssueDateBeforeRenewal, bool isValid)
            {
                return new object[]  { certStart,  certEnd,  timeUntilExpiryBeforeRenewal,  timeAfterIssueDateBeforeRenewal,  isValid };
            }
        }
    }
}