using System;
using System.Linq;
using System.Threading.Tasks;
using Certes;
using Certes.Acme;
using Certes.Acme.Resource;
using FluffySpoon.AspNet.EncryptWeMust.Exceptions;
using FluffySpoon.AspNet.EncryptWeMust.Persistence;
using Microsoft.Extensions.Logging;

namespace FluffySpoon.AspNet.EncryptWeMust.Certes
{
    public interface ILetsEncryptClient
    {
        Task<PlacedOrder> PlaceOrder(string[] domains);
        Task<PfxCertificate> FinalizeOrder(PlacedOrder placedOrder);
    }
    
    public class LetsEncryptClient : ILetsEncryptClient
    {
        private const string CertificateFriendlyName = "FluffySpoonAspNetLetsEncryptCertificate";

        private readonly ILogger _logger;
        private readonly IAcmeContext _acme;
        private readonly LetsEncryptOptions _options;

        public LetsEncryptClient(IAcmeContext acme, LetsEncryptOptions options, ILogger logger)
        {
            _logger = logger;
            _acme = acme;
            _options = options;
        }

        public async Task<PlacedOrder> PlaceOrder(string[] domains)
        {
            _logger.LogInformation("Ordering LetsEncrypt certificate for domains {Domains}.", (object)domains);
            var order = await _acme.NewOrder(domains);

            var allAuthorizations = await order.Authorizations();

            var challengeContexts = await Task.WhenAll(allAuthorizations.Select(x => x.Http()));
            var nonNullChallengeContexts = challengeContexts.Where(x => x != null).ToArray();
            
            var dtos = nonNullChallengeContexts.Select(x => new ChallengeDto
            {
                Token = x.Type == ChallengeTypes.Dns01 ? _acme.AccountKey.DnsTxt(x.Token) : x.Token,
                Response = x.KeyAuthz,
                Domains = domains
            }).ToArray();
            
            _logger.LogTrace("LetsEncrypt placed order for domains {Domains} with challenges {Challenges}", domains, dtos);
            
            return new PlacedOrder(dtos, order, nonNullChallengeContexts);
        }

        public async Task<PfxCertificate> FinalizeOrder(PlacedOrder placedOrder)
        {
            await ValidateChallenges(placedOrder.ChallengeContexts);
            var bytes = await AcquireCertificateBytesFromOrderAsync(placedOrder.Order);
            return new PfxCertificate(bytes);
        }

        private async Task ValidateChallenges(IChallengeContext[] challengeContexts)
        {
            _logger.LogInformation("Validating all pending order authorizations.");

            var challengeValidationResponses = await ValidateChallengesAsync(challengeContexts);
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

        private async Task<byte[]> AcquireCertificateBytesFromOrderAsync(IOrderContext order)
        {
            _logger.LogInformation("Acquiring certificate through signing request.");

            var keyPair = KeyFactory.NewKey(_options.KeyAlgorithm);
			
            var certificateChain = await order.Generate(_options.CertificateSigningRequest, keyPair, _options.PreferredChain);

            var pfxBuilder = certificateChain.ToPfx(keyPair);
			
            pfxBuilder.FullChain = true;

            var pfxBytes = pfxBuilder.Build(CertificateFriendlyName, nameof(FluffySpoon));

            _logger.LogInformation("Certificate acquired.");

            return pfxBytes;
        }

        private static async Task<Challenge[]> ValidateChallengesAsync(IChallengeContext[] challengeContexts)
        {
            var challenges = await Task.WhenAll(challengeContexts.Select(x => x.Validate()));

            while (true)
            {
                var allValid = challenges.All(x => x.Status == ChallengeStatus.Valid);
                var anyInvalid = challenges.Any(x => x.Status == ChallengeStatus.Invalid);
				
                if (allValid || anyInvalid)
                    break;
                
                await Task.Delay(1000);
                challenges = await Task.WhenAll(challengeContexts.Select(x => x.Resource()));
            }

            return challenges;
        }
    }
}