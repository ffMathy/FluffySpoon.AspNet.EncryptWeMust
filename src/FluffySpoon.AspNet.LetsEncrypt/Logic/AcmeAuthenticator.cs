using System.Threading.Tasks;
using Certes;
using FluffySpoon.AspNet.LetsEncrypt.Persistence;
using Microsoft.Extensions.Logging;

namespace FluffySpoon.AspNet.LetsEncrypt.Logic
{
    public class AcmeAuthenticator : IAcmeAuthenticator
    {
        private readonly LetsEncryptOptions _options;
        private readonly IPersistenceService _persistence;
        private readonly ILogger<AcmeAuthenticator> _logger;
        
        public AcmeAuthenticator(
            LetsEncryptOptions options,
            IPersistenceService persistence,
            ILogger<AcmeAuthenticator> logger)
        {
            _options = options;
            _persistence = persistence;
            _logger = logger;
        }
        
        // TODO: ensure singleton semantics
        private IAcmeContext _acme;

        public async Task<IAcmeContext> AuthenticateAsync()
        {
            if (_acme != null)
                return _acme;

            var existingAccountKey = await _persistence.GetPersistedAccountCertificateAsync();

            if (existingAccountKey != null)
                return _acme = await UseExistingLetsEncryptAccount(existingAccountKey);
            
            return _acme = await CreateNewLetsEncryptAccount();
        }
        
        private async Task<AcmeContext> UseExistingLetsEncryptAccount(IKey key)
        {
            _logger.LogDebug("Using existing LetsEncrypt account.");
            var acme = new AcmeContext(_options.LetsEncryptUri, key);
            await acme.Account();
            return acme;
        }

        private async Task<AcmeContext> CreateNewLetsEncryptAccount()
        {
            _logger.LogDebug("Creating LetsEncrypt account with email {0}.", _options.Email);
            var acme = new AcmeContext(_options.LetsEncryptUri);
            await acme.NewAccount(_options.Email, true);
            await _persistence.PersistAccountCertificateAsync(acme.AccountKey);
            return acme; 
        }
    }
}