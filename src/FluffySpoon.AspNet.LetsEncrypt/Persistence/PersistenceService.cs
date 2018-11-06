using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Certes;
using FluffySpoon.AspNet.LetsEncrypt.Persistence.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace FluffySpoon.AspNet.LetsEncrypt.Persistence
{
	class PersistenceService : IPersistenceService
	{
		private const string AccountCertificateKey = "AccountCertificate";
		private const string SiteCertificateKey = "SiteCertificate";
		private const string ChallengeKey = "Challenges";

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
			await PersistAsync(AccountCertificateKey, bytes, _certificatePersistenceStrategies);
		}

		public async Task PersistSiteCertificateAsync(X509Certificate2 certificate)
		{
			await PersistAsync(SiteCertificateKey, certificate.RawData, _certificatePersistenceStrategies);
			_logger.LogInformation("Certificate persisted for later use.");
		}

		public async Task PersistChallengesAsync(ChallengeDto[] challenges)
		{
			var text = challenges == null ? null : JsonConvert.SerializeObject(challenges);
			var bytes = text == null ? null : Encoding.UTF8.GetBytes(text);
			await PersistAsync(ChallengeKey, bytes, _challengePersistenceStrategies);
		}

		private async Task PersistAsync(string key, byte[] bytes, IEnumerable<IPersistenceStrategy> strategies) {
			var tasks = strategies.Select(x => x.PersistAsync(key, bytes ?? new byte[0]));
			await Task.WhenAll(tasks);
		}

		public async Task<X509Certificate2> GetPersistedSiteCertificateAsync()
		{
			var bytes = await GetPersistedBytesAsync(SiteCertificateKey, _certificatePersistenceStrategies);
			return new X509Certificate2(bytes);
		}

		public async Task<IKey> GetPersistedAccountCertificateAsync()
		{
			var bytes = await GetPersistedBytesAsync(AccountCertificateKey, _certificatePersistenceStrategies);
			var text = Encoding.UTF8.GetString(bytes);
			return KeyFactory.FromPem(text);
		}

		public async Task<ChallengeDto[]> GetPersistedChallengesAsync()
		{
			var bytes = await GetPersistedBytesAsync(ChallengeKey, _challengePersistenceStrategies);
			var text = Encoding.UTF8.GetString(bytes);
			return JsonConvert.DeserializeObject<ChallengeDto[]>(text);
		}

		private async Task<byte[]> GetPersistedBytesAsync(string key, IEnumerable<IPersistenceStrategy> strategies)
		{
			foreach (var strategy in strategies)
			{
				var bytes = await strategy.RetrieveAsync(key);
				if (bytes != null && bytes.Length > 0)
					return bytes;
			}

			return null;
		}
	}
}
