using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Certes;
using FluffySpoon.AspNet.LetsEncrypt.Persistence.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace FluffySpoon.AspNet.LetsEncrypt.Persistence
{
	public enum PersistenceType {
		Account,
		Site,
		Challenges
	}

	class PersistenceService : IPersistenceService
	{
		private const string DnsChallengeNameFormat = "_acme-challenge.{0}";
		private const string WildcardRegex = "^\\*\\.";
		private const string TxtRecordType = "TXT";

		private readonly IEnumerable<ICertificatePersistenceStrategy> _certificatePersistenceStrategies;
		private readonly IEnumerable<IChallengePersistenceStrategy> _challengePersistenceStrategies;
		private readonly IEnumerable<IDnsChallengePersistenceStrategy> _dnsChallengePersistenceStrategies;

		private readonly ILogger<IPersistenceService> _logger;

		public PersistenceService(
			IEnumerable<ICertificatePersistenceStrategy> certificatePersistenceStrategies,
			IEnumerable<IChallengePersistenceStrategy> challengePersistenceStrategies,
			IEnumerable<IDnsChallengePersistenceStrategy> dnsChallengePersistenceStrategies,
			ILogger<IPersistenceService> logger)
		{
			_certificatePersistenceStrategies = certificatePersistenceStrategies;
			_challengePersistenceStrategies = challengePersistenceStrategies;
			_dnsChallengePersistenceStrategies = dnsChallengePersistenceStrategies;
			_logger = logger;
		}

		public async Task PersistAccountCertificateAsync(IKey certificate)
		{
			var text = certificate.ToPem();
			var bytes = Encoding.UTF8.GetBytes(text);
			await PersistAsync(PersistenceType.Account, bytes, _certificatePersistenceStrategies);
		}

		public async Task PersistSiteCertificateAsync(byte[] rawCertificate)
		{
			await PersistAsync(PersistenceType.Site, rawCertificate, _certificatePersistenceStrategies);
			_logger.LogInformation("Certificate persisted for later use.");
		}

		public async Task PersistChallengesAsync(ChallengeDto[] challenges)
		{
			var httpChallenges = challenges?.Where(x => x.Type == ChallengeType.Http01).ToArray();
			var json = httpChallenges == null ? null : JsonConvert.SerializeObject(httpChallenges);
			_logger.LogDebug("Persisting challenges {0}", json);

			var bytes = json == null ? null : Encoding.UTF8.GetBytes(json);
			await PersistAsync(PersistenceType.Challenges, bytes, _challengePersistenceStrategies);

			if (challenges == null)
				return;

			var dnsChallenges = challenges.Where(x => x.Type == ChallengeType.Dns01);
			foreach (var dnsChallenge in dnsChallenges)
			{
				_logger.LogTrace("Persisting DNS challenge through {0} possible strategies", _dnsChallengePersistenceStrategies.Count());

				foreach (var domain in dnsChallenge.Domains) {
					var dnsName = Regex.Replace(domain, WildcardRegex, String.Empty);
					dnsName = String.Format(DnsChallengeNameFormat, dnsName);

					var tasks = _dnsChallengePersistenceStrategies.Select(x => x.PersistAsync(dnsName, TxtRecordType, dnsChallenge.Token));
					await Task.WhenAll(tasks);
				}
			}
		}

		public async Task DeleteChallengesAsync(ChallengeDto[] challenges)
		{
			var dnsChallenges = challenges?.Where(x => x.Type == ChallengeType.Dns01);
			foreach (var dnsChallenge in dnsChallenges)
			{
				_logger.LogTrace("Deleting DNS challenge through {0} possible strategies", _dnsChallengePersistenceStrategies.Count());

				foreach (var domain in dnsChallenge.Domains)
				{
					var dnsName = Regex.Replace(domain, WildcardRegex, String.Empty);
					dnsName = String.Format(DnsChallengeNameFormat, dnsName);

					var tasks = _dnsChallengePersistenceStrategies.Select(x => x.DeleteAsync(dnsName, TxtRecordType));
					await Task.WhenAll(tasks);
				}
			}
		}

		private async Task PersistAsync(PersistenceType persistenceType, byte[] bytes, IEnumerable<IPersistenceStrategy> strategies) {
			_logger.LogTrace("Persisting {0} through strategies.", persistenceType);

			var tasks = strategies.Select(x => x.PersistAsync(persistenceType, bytes ?? new byte[0]));
			await Task.WhenAll(tasks);
		}

		public async Task<X509Certificate2> GetPersistedSiteCertificateAsync()
		{
			var bytes = await GetPersistedBytesAsync(PersistenceType.Site, _certificatePersistenceStrategies);
			if (bytes == null)
				return null;

			return new X509Certificate2(bytes, nameof(FluffySpoon));
		}

		public async Task<IKey> GetPersistedAccountCertificateAsync()
		{
			var bytes = await GetPersistedBytesAsync(PersistenceType.Account, _certificatePersistenceStrategies);
			if(bytes == null)
				return null;

			var text = Encoding.UTF8.GetString(bytes);
			return KeyFactory.FromPem(text);
		}

		public async Task<ChallengeDto[]> GetPersistedChallengesAsync()
		{
			var bytes = await GetPersistedBytesAsync(PersistenceType.Challenges, _challengePersistenceStrategies);
			if(bytes == null)
				return Array.Empty<ChallengeDto>();

			var text = Encoding.UTF8.GetString(bytes);
			return JsonConvert.DeserializeObject<ChallengeDto[]>(text);
		}

		private async Task<byte[]> GetPersistedBytesAsync(PersistenceType persistenceType, IEnumerable<IPersistenceStrategy> strategies)
		{
			foreach (var strategy in strategies)
			{
				var bytes = await strategy.RetrieveAsync(persistenceType);
				if (bytes != null && bytes.Length > 0)
					return bytes;
			}

			return null;
		}
	}
}
