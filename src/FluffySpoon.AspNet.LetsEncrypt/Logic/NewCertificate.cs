using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Certes;
using FluffySpoon.AspNet.LetsEncrypt.Persistence;
using Microsoft.Extensions.Logging;

namespace FluffySpoon.AspNet.LetsEncrypt.Logic
{
	public interface INewCertificate
	{
		Task<X509Certificate2> RequestNewLetsEncryptCertificate();
	}

	public class NewCertificate : INewCertificate
	{
		private readonly LetsEncryptOptions _options;
		private readonly IPersistenceService _persistenceService;
		private readonly ILetsEncryptClient _client;
		private readonly ILogger<NewCertificate> _logger;
		private readonly string[] _domains;

		private IAcmeContext _acme;
		
		public NewCertificate(
			LetsEncryptOptions options,
			IPersistenceService persistenceService,
			ILetsEncryptClient client,
			ILogger<NewCertificate> logger)
		{

			var domains = options.Domains?.Distinct().ToArray();
			if (domains == null || domains.Length == 0)
			{
				throw new ArgumentException("Domains configuration invalid");
			}

			_domains = domains;
			_options = options;
			_persistenceService = persistenceService;
			_client = client;
			_logger = logger;
		}

		public async Task<X509Certificate2> RequestNewLetsEncryptCertificate()
		{
			var acmeContext = await GetAcmeContext();

			var placedOrder = await _client.PlaceOrder(_domains, acmeContext);

			await _persistenceService.PersistChallengesAsync(placedOrder.ChallengeDtos);

			try
			{
				var pfxCertificateBytes = await _client.FinalizeOrder(placedOrder);

				await _persistenceService.PersistSiteCertificateAsync(pfxCertificateBytes.Data);

				const string password = nameof(FluffySpoon);
				
				return new X509Certificate2(pfxCertificateBytes.Data, password);
			}
			finally
			{
				await _persistenceService.DeleteChallengesAsync(placedOrder.ChallengeDtos);
			}
		}
		
		private async Task<IAcmeContext> GetAcmeContext()
		{
			if (_acme != null)
				return _acme;

			var existingAccountKey = await _persistenceService.GetPersistedAccountCertificateAsync();
			if (existingAccountKey != null)
			{
				_logger.LogDebug("Using existing LetsEncrypt account.");
				var acme = new AcmeContext(_options.LetsEncryptUri, existingAccountKey);
				await acme.Account();
				return _acme = acme;
			}
			else
			{
				_logger.LogDebug("Creating LetsEncrypt account with email {0}.", _options.Email);
				var acme = new AcmeContext(_options.LetsEncryptUri);
				await acme.NewAccount(_options.Email, true);
				await _persistenceService.PersistAccountCertificateAsync(acme.AccountKey);
				return _acme = acme;
			}
		}
	}
}