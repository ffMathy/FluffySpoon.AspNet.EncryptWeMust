using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Certes;
using Certes.Acme;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace FluffySpoon.AspNet.LetsEncrypt
{
	public class LetsEncryptMiddleware
	{
		private readonly RequestDelegate _next;
		private readonly LetsEncryptOptions _options;
		private readonly ILogger<LetsEncryptMiddleware> _logger;
		private readonly LetsEncryptCertificateContainer _stateContainer;

		private readonly HashSet<string> _domainsForFastLookup;

		public LetsEncryptMiddleware(
			RequestDelegate next, 
			LetsEncryptOptions options,
			ILogger<LetsEncryptMiddleware> logger,
			LetsEncryptCertificateContainer stateContainer)
		{
			if (options?.Domains == null)
				throw new ArgumentNullException(nameof(options), "You must provide what domains to use for LetsEncrypt.");

			if (options?.Domains == null)
				throw new ArgumentNullException(nameof(options), "You must provide an e-mail address to use for LetsEncrypt.");

			if (options?.CertificateSigningRequest == null)
				throw new ArgumentNullException(nameof(options), "You must provide a certificate signing request to use for LetsEncrypt.");

			_next = next;
			_options = options;
			_logger = logger;
			_stateContainer = stateContainer;

			_domainsForFastLookup = new HashSet<string>(options.Domains);
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
				var matchingChallenge = _stateContainer.PendingChallengeContexts.FirstOrDefault(x => x.Token == requestedToken);
				if (matchingChallenge == null)
				{
					_logger.LogInformation("The given challenge did not match: {0}", path);

					await _next(context);
					return;
				}

				await context.Response.WriteAsync(matchingChallenge.KeyAuthz);
				return;
			}

			await _next(context);
		}
	}
}
