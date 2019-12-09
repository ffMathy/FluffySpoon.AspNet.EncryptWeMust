using System.Linq;
using System.Threading.Tasks;
using FluffySpoon.AspNet.LetsEncrypt.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace FluffySpoon.AspNet.LetsEncrypt
{
	public class LetsEncryptChallengeApprovalMiddleware : ILetsEncryptChallengeApprovalMiddleware
	{
        private const string MagicPrefix = "/.well-known/acme-challenge/";

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
            var path = context.Request.Path;
            if (path.HasValue && path.Value.StartsWith(MagicPrefix))
			{
                return ProcessAcmeChallenge(context);
            }

            return _next(context);
        }

        private async Task ProcessAcmeChallenge(HttpContext context)
        {
            var path = context.Request.Path.ToString();
            _logger.LogDebug("Challenge invoked: {challengePath}", path);

            var requestedToken = path.Substring(MagicPrefix.Length);
            var allChallenges = await _persistenceService.GetPersistedChallengesAsync();
            var matchingChallenge = allChallenges.FirstOrDefault(x => x.Token == requestedToken);
            if (matchingChallenge == null)
            {
                _logger.LogInformation("The given challenge did not match {challengePath} among {allChallenges}", path, allChallenges);

                await _next(context);
                return;
            }

            await context.Response.WriteAsync(matchingChallenge.Response);
        }
    }
}
