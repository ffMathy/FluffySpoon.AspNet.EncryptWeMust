﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Certes;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace FluffySpoon.AspNet.LetsEncrypt.Persistence
{
	public enum CertificateType {
		Account,
		Site
	}

	class PersistenceService : IPersistenceService
	{
		private const string DnsChallengeNameFormat = "_acme-challenge.{0}";
		private const string WildcardRegex = "^\\*\\.";

		private readonly IEnumerable<ICertificatePersistenceStrategy> _certificatePersistenceStrategies;
		private readonly IEnumerable<IChallengePersistenceStrategy> _challengePersistenceStrategies;

		private readonly ILogger<IPersistenceService> _logger;

		public PersistenceService(
			IEnumerable<ICertificatePersistenceStrategy> certificatePersistenceStrategies,
			IEnumerable<IChallengePersistenceStrategy> challengePersistenceStrategies,
			ILogger<IPersistenceService> logger)
		{
			_certificatePersistenceStrategies = certificatePersistenceStrategies;
			_challengePersistenceStrategies = challengePersistenceStrategies;
			_logger = logger;
		}

		public async Task PersistAccountCertificateAsync(IKey certificate)
		{
			var text = certificate.ToPem();
			var bytes = Encoding.UTF8.GetBytes(text);
			await PersistCertificateAsync(CertificateType.Account, bytes, _certificatePersistenceStrategies);
		}

		public async Task PersistSiteCertificateAsync(byte[] rawCertificate)
		{
			await PersistCertificateAsync(CertificateType.Site, rawCertificate, _certificatePersistenceStrategies);
			_logger.LogInformation("Certificate persisted for later use.");
		}

		public async Task PersistChallengesAsync(ChallengeDto[] challenges)
		{
			_logger.LogTrace("Using ({Strategies}) for persisting challenge", (object) _challengePersistenceStrategies);
			await PersistChallengesAsync(challenges, _challengePersistenceStrategies);
		}

		public async Task DeleteChallengesAsync(ChallengeDto[] challenges)
		{
			await DeleteChallengesAsync(challenges, _challengePersistenceStrategies);
		}

		private string GetChallengeDnsName(string domain)
		{
			var dnsName = Regex.Replace(domain, WildcardRegex, String.Empty);
			dnsName = String.Format(DnsChallengeNameFormat, dnsName);

			return dnsName;
		}

		private async Task PersistCertificateAsync(CertificateType persistenceType, byte[] bytes, IEnumerable<ICertificatePersistenceStrategy> strategies) 
		{
			_logger.LogTrace("Persisting {type} certificate through strategies", persistenceType);

			var tasks = strategies.Select(x => x.PersistAsync(persistenceType, bytes ?? new byte[0]));
			await Task.WhenAll(tasks);
		}

		private async Task PersistChallengesAsync(IEnumerable<ChallengeDto> challenges, IEnumerable<IChallengePersistenceStrategy> strategies)
		{
			_logger.LogTrace("Persisting challenges ({challenges}) through strategies.", challenges);

			if (!strategies.Any())
			{
				_logger.LogWarning("There are no challenges persistence strategies - challenges will not be stored");
			}

			var tasks = strategies.Select(x =>
				x.PersistAsync(challenges));

			await Task.WhenAll(tasks);
		}

		public async Task<X509Certificate2> GetPersistedSiteCertificateAsync()
		{
			var bytes = await GetPersistedCertificateBytesAsync(CertificateType.Site, _certificatePersistenceStrategies);
			if (bytes == null)
				return null;

			return new X509Certificate2(bytes, nameof(FluffySpoon));
		}

		public async Task<IKey> GetPersistedAccountCertificateAsync()
		{
			var bytes = await GetPersistedCertificateBytesAsync(CertificateType.Account, _certificatePersistenceStrategies);
			if (bytes == null)
				return null;

			var text = Encoding.UTF8.GetString(bytes);
			return KeyFactory.FromPem(text);
		}

		public async Task<ChallengeDto[]> GetPersistedChallengesAsync()
		{
			var challenges = await GetPersistedChallengesAsync(_challengePersistenceStrategies);
			return challenges.ToArray();
		}

		private async Task<IEnumerable<ChallengeDto>> GetPersistedChallengesAsync(IEnumerable<IChallengePersistenceStrategy> strategies)
		{
			var result = new List<ChallengeDto>();
			foreach (var strategy in strategies)
				result.AddRange(await strategy.RetrieveAsync());

			if (!result.Any())
			{
				_logger.LogWarning("There are no persisted challenges from strategies {strategies}",
					string.Join(",", strategies));
			}
			else
			{
				_logger.LogTrace("Retrieved challenges {challenges} from persistence strategies", result);
			}

			return result;
		}

		private async Task<byte[]> GetPersistedCertificateBytesAsync(CertificateType persistenceType, IEnumerable<ICertificatePersistenceStrategy> strategies)
		{
			foreach (var strategy in strategies)
			{
				var bytes = await strategy.RetrieveAsync(persistenceType);
				if (bytes != null && bytes.Length > 0)
					return bytes;
			}

			_logger.LogTrace("Did not find certificate of type {type} with strategies {strategies}.", persistenceType, string.Join(",", strategies));

			return null;
		}

		private async Task DeleteChallengesAsync(IEnumerable<ChallengeDto> challenges, IEnumerable<IChallengePersistenceStrategy> strategies)
		{
			_logger.LogTrace("Deleting challenges {challenges} through strategies.", challenges);

			var tasks = strategies.Select(x =>
				x.DeleteAsync(
					challenges));

			await Task.WhenAll(tasks);
		}
	}
}
