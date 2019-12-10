using System;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Certes;
using Certes.Acme;
using Certes.Acme.Resource;
using FluffySpoon.AspNet.LetsEncrypt.Exceptions;
using FluffySpoon.AspNet.LetsEncrypt.Persistence;
using FluffySpoon.AspNet.LetsEncrypt.Persistence.Models;
using Microsoft.Extensions.Logging;

namespace FluffySpoon.AspNet.LetsEncrypt
{
	public class CertificateRenewal : ICertificateRenewal
	{
		public const string CertificateFriendlyName = "FluffySpoonAspNetLetsEncryptCertificate";

		private readonly IPersistenceService _persistenceService;
		private readonly ILogger<ICertificateRenewal> _logger;
		private readonly LetsEncryptOptions _options;

		private IAcmeContext _acme;

		public CertificateRenewal(
			IPersistenceService persistenceService,
			ILogger<ICertificateRenewal> logger,
			LetsEncryptOptions options)
		{
			_persistenceService = persistenceService;
			_logger = logger;
			_options = options;
		}

        public async Task<X509Certificate2> RenewCertificateIfNeeded(X509Certificate2 current)
		{
			_logger.LogInformation("Checking to see if in-memory LetsEncrypt certificate needs renewal.");

			if (IsCertificateValid(current))
			{
				_logger.LogInformation("Current in-memory LetsEncrypt certificate is valid.");
				return current;
			}

			_logger.LogInformation("Checking to see if existing LetsEncrypt certificate has been persisted and is valid.");

			var persistedSiteCertificate = await _persistenceService.GetPersistedSiteCertificateAsync();
			if (IsCertificateValid(persistedSiteCertificate))
			{
				_logger.LogInformation("A persisted non-expired LetsEncrypt certificate was found and will be used.");
				return persistedSiteCertificate;
			}

			_logger.LogInformation("A persisted but expired LetsEncrypt certificate was found and will be renewed.");

			await AuthenticateAsync();

			var domains = _options.Domains?.ToArray() ?? Array.Empty<string>();
			
			return await AcquireNewCertificateForDomains(domains);
		}

        private bool IsCertificateValid(X509Certificate2 certificate)
        {
	        try
	        {
		        if (certificate == null)
			        return false;
		        else if (_options.TimeUntilExpiryBeforeRenewal != null && certificate.NotAfter - DateTime.Now < _options.TimeUntilExpiryBeforeRenewal)
			        return false;
		        else if (_options.TimeAfterIssueDateBeforeRenewal != null && DateTime.Now - certificate.NotBefore > _options.TimeAfterIssueDateBeforeRenewal)
			        return false;
		        else if (certificate.NotBefore > DateTime.Now || certificate.NotAfter < DateTime.Now)
			        return false;
		        else
			        return true;
	        }
	        catch (CryptographicException exc)
	        {
		        _logger.LogError(exc, "Exception occured during certificate validation");
		        return false;
	        }
        }

        private async Task<X509Certificate2> AcquireNewCertificateForDomains(string[] domains)
		{
			_logger.LogInformation("Ordering LetsEncrypt certificate for domains {0}.", new object[] { domains });

			var order = await _acme.NewOrder(domains);
			await ValidateOrderAsync(domains, order);

			var certificateBytes = await AcquireCertificateBytesFromOrderAsync(order);
			if (certificateBytes == null)
			{
				throw new InvalidOperationException("The certificate from the order was null.");
			}

			await _persistenceService.PersistSiteCertificateAsync(certificateBytes);

			return new X509Certificate2(certificateBytes, nameof(FluffySpoon));
		}

		private async Task AuthenticateAsync()
		{
			if (_acme != null)
				return;

			var existingAccountKey = await _persistenceService.GetPersistedAccountCertificateAsync();
			if (existingAccountKey != null)
			{
				await UseExistingLetsEncryptAccount(existingAccountKey);
			}
			else
			{
				await CreateNewLetsEncryptAccount();
			}
		}

		private async Task UseExistingLetsEncryptAccount(IKey existingAccountKey)
		{
			_logger.LogDebug("Using existing LetsEncrypt account.");

			_acme = new AcmeContext(_options.LetsEncryptUri, existingAccountKey);
			
			await _acme.Account();
		}

		private async Task CreateNewLetsEncryptAccount()
		{
			_logger.LogDebug("Creating LetsEncrypt account with email {0}.", _options.Email);

			_acme = new AcmeContext(_options.LetsEncryptUri);
			
			await _acme.NewAccount(_options.Email, true);

			await _persistenceService.PersistAccountCertificateAsync(_acme.AccountKey);
		}

		private async Task<byte[]> AcquireCertificateBytesFromOrderAsync(IOrderContext order)
		{
			_logger.LogInformation("Acquiring certificate through signing request.");

			var keyPair = KeyFactory.NewKey(_options.KeyAlgorithm);
			
            var certificateChain = await order.Generate(_options.CertificateSigningRequest, keyPair);

			var pfxBuilder = certificateChain.ToPfx(keyPair);
			
            pfxBuilder.FullChain = true;

			var pfxBytes = pfxBuilder.Build(CertificateFriendlyName, nameof(FluffySpoon));

			_logger.LogInformation("Certificate acquired.");

			return pfxBytes;
		}

		private async Task ValidateOrderAsync(string[] domains, IOrderContext order)
		{
			var allAuthorizations = await order.Authorizations();
            var challengeContexts = await Task.WhenAll(allAuthorizations.Select(x => x.Http()));
			var nonNullChallengeContexts = challengeContexts.Where(x => x != null).ToArray();

			_logger.LogInformation("Validating all pending order authorizations.");

			var challengeDtos = nonNullChallengeContexts.Select(x => new ChallengeDto
			{
				Token = x.Type == ChallengeTypes.Dns01 ? _acme.AccountKey.DnsTxt(x.Token) : x.Token,
				Response = x.KeyAuthz,
				Domains = domains
			}).ToArray();

			await _persistenceService.PersistChallengesAsync(challengeDtos);

			try
			{
				var challengeValidationResponses = await ValidateChallengesAsync(nonNullChallengeContexts);
				var nonNullChallengeValidationResponses = challengeValidationResponses.Where(x => x != null).ToArray();

				if (challengeValidationResponses.Length > nonNullChallengeValidationResponses.Length)
					_logger.LogWarning("Some challenge responses were null.");
							   
				var challengeExceptions = nonNullChallengeValidationResponses
					.Where(x => x.Status == ChallengeStatus.Invalid)
					.Select(x => new Exception($"{x.Error?.Type ?? "errortype null"}: {x.Error?.Detail ?? "null errordetails"} (challenge type {x.Type ?? "null"})"))
					.ToArray();

				if (challengeExceptions.Length > 0) 
					throw new OrderInvalidException(
						"One or more LetsEncrypt orders were invalid. Make sure that LetsEncrypt can contact the domain you are trying to request an SSL certificate for, in order to verify it.",
						new AggregateException(challengeExceptions));
			}
			finally
			{
				await _persistenceService.DeleteChallengesAsync(challengeDtos);
			}
		}

		private static async Task<Challenge[]> ValidateChallengesAsync(IChallengeContext[] challengeContexts)
		{
			var challenges = await Task.WhenAll(challengeContexts.Select(x => x.Validate()));

			while (true)
			{
				var anyValid = challenges.Any(x => x.Status == ChallengeStatus.Valid);
				var allInvalid = challenges.All(x => x.Status == ChallengeStatus.Invalid);
				
				if (anyValid || allInvalid)
					break;
                
                await Task.Delay(1000);
				challenges = await Task.WhenAll(challengeContexts.Select(x => x.Resource()));
			}

			return challenges;
		}
	}
}