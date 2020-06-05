using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Certes;
using FluffySpoon.AspNet.EncryptWeMust.Certificates;
using Microsoft.Extensions.Logging;

namespace FluffySpoon.AspNet.EncryptWeMust.Persistence
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
			await PersistCertificateAsync(CertificateType.Account, new AccountKeyCertificate(certificate), _certificatePersistenceStrategies);
		}

		public async Task PersistSiteCertificateAsync(IPersistableCertificate certificate)
		{
			await PersistCertificateAsync(CertificateType.Site, certificate, _certificatePersistenceStrategies);
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

		private async Task PersistCertificateAsync(CertificateType persistenceType, IPersistableCertificate certificate,
			IEnumerable<ICertificatePersistenceStrategy> strategies) 
		{
			_logger.LogTrace("Persisting {type} certificate through strategies", persistenceType);

			var tasks = strategies.Select(x => x.PersistAsync(persistenceType, certificate));
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

		public async Task<IAbstractCertificate> GetPersistedSiteCertificateAsync()
		{
			foreach (var strategy in _certificatePersistenceStrategies)
			{
				var certificate = await strategy.RetrieveSiteCertificateAsync();
				if (certificate != null)
					return certificate;
			}

			_logger.LogTrace("Did not find site certificate with strategies {strategies}.", string.Join(",", _certificatePersistenceStrategies));
			return null;
		}

		public async Task<IKey> GetPersistedAccountCertificateAsync()
		{
			foreach (var strategy in _certificatePersistenceStrategies)
			{
				var certificate = await strategy.RetrieveAccountCertificateAsync();
				if (certificate != null)
				{
					return certificate.Key;
				}
			}

			_logger.LogTrace("Did not find account certificate with strategies {strategies}.", string.Join(",", _certificatePersistenceStrategies));
			return null;
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
