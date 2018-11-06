using System.Linq;
using System.Threading.Tasks;
using FluffySpoon.AspNet.LetsEncrypt.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace FluffySpoon.AspNet.LetsEncrypt
{
	public class LetsEncryptChallengeApprovalMiddleware : ILetsEncryptChallengeApprovalMiddleware1
	{
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

		public async Task InvokeAsync(
			HttpContext context)
		{
			var path = context.Request.Path.ToString();

			const string magicPrefix = "/.well-known/acme-challenge/";
			if (path.StartsWith(magicPrefix))
			{
				_logger.LogDebug("Challenge invoked: {0}", path);

				var requestedToken = path.Substring(magicPrefix.Length);

				var allChallenges = await _persistenceService.GetPersistedChallengesAsync();
				var matchingChallenge = allChallenges.FirstOrDefault(x => x.Token == requestedToken);
				if (matchingChallenge == null)
				{
					_logger.LogInformation("The given challenge did not match: {0}", path);

					await _next(context);
					return;
				}

				await context.Response.WriteAsync(matchingChallenge.Response);
				return;
			}

			await _next(context);
		}
	}
}
