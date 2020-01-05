using System.Linq;
using System.Threading.Tasks;
using FluffySpoon.AspNet.LetsEncrypt.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace FluffySpoon.AspNet.LetsEncrypt.Certes
{
    public class LetsEncryptChallengeApprovalMiddleware : ILetsEncryptChallengeApprovalMiddleware
    {

        private const string MagicPrefix = "/.well-known/acme-challenge";
        private static readonly PathString MagicPrefixSegments = new PathString(MagicPrefix);

        private readonly RequestDelegate _next;
        private readonly ILogger<ILetsEncryptChallengeApprovalMiddleware> _logger;
        private readonly IPersistenceService _persistenceService;

        public LetsEncryptChallengeApprovalMiddleware(
            RequestDelegate next,
            ILogger<ILetsEncryptChallengeApprovalMiddleware> logger,
            IPersistenceService persistenceService)
        {
            _next = next;
            _logger = logger;
            _persistenceService = persistenceService;
        }

        public Task InvokeAsync(HttpContext context)
        {
            if (context.Request.Path.StartsWithSegments(MagicPrefixSegments))
            {
                return ProcessAcmeChallenge(context);
            }

            return _next(context);
        }

        private async Task ProcessAcmeChallenge(HttpContext context)
        {
            var path = context.Request.Path.ToString();
            _logger.LogDebug("Challenge invoked: {challengePath}", path);

            var requestedToken = path.Substring($"{MagicPrefix}/".Length);
            var allChallenges = await _persistenceService.GetPersistedChallengesAsync();
            var matchingChallenge = allChallenges.FirstOrDefault(x => x.Token == requestedToken);
            if (matchingChallenge == null)
            {
                _logger.LogInformation("The given challenge did not match {challengePath} among {allChallenges}", path, allChallenges);
                await _next(context);
                return;
            }

            // token response is always in ASCII so char count would be equal to byte count here
            context.Response.ContentLength = matchingChallenge.Response.Length;
            context.Response.ContentType = "application/octet-stream";
            await context.Response.WriteAsync(
                text: matchingChallenge.Response,
                cancellationToken: context.RequestAborted);
        }
    }
}
