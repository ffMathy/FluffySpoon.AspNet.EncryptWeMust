using System.Threading.Tasks;
using Certes;
using FluffySpoon.AspNet.LetsEncrypt.Persistence;
using Microsoft.Extensions.Logging;

namespace FluffySpoon.AspNet.LetsEncrypt.Certes
{
    public interface ILetsEncryptClientFactory
    {
        Task<ILetsEncryptClient> GetClient();
    }

    public class LetsEncryptClientFactory : ILetsEncryptClientFactory
    {
        private readonly LetsEncryptOptions _options;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger _logger;
        private readonly IPersistenceService _persistenceService;
        private AcmeContext _acme;
        
        public LetsEncryptClientFactory(
            IPersistenceService persistenceService,
            LetsEncryptOptions options,
            ILoggerFactory loggerFactory)
        {
            _options = options;
            _loggerFactory = loggerFactory;
            _persistenceService = persistenceService;
            _logger = loggerFactory.CreateLogger<LetsEncryptClientFactory>();
        }

        public async Task<ILetsEncryptClient> GetClient()
        {
            var context = await GetContext();
            var logger = _loggerFactory.CreateLogger<LetsEncryptClient>();
            return new LetsEncryptClient(context, _options, logger);
        }

        private async Task<IAcmeContext> GetContext()
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
                _logger.LogDebug("Creating LetsEncrypt account with email {EmailAddress}.", _options.Email);
                var acme = new AcmeContext(_options.LetsEncryptUri);
                await acme.NewAccount(_options.Email, true);
                await _persistenceService.PersistAccountCertificateAsync(acme.AccountKey);
                return _acme = acme;
            }
        }
    }
}