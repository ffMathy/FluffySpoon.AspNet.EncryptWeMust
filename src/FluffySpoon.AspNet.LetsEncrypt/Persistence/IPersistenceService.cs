using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Certes;
using FluffySpoon.AspNet.LetsEncrypt.Persistence.Models;

namespace FluffySpoon.AspNet.LetsEncrypt.Persistence
{
	public interface IPersistenceService
	{
		bool HasHttpChallengePersistenceStrategies();
		bool HasDnsChallengePersistenceStrategies();
		Task<IKey> GetPersistedAccountCertificateAsync();
		Task<ChallengeDto[]> GetPersistedChallengesAsync();
		Task<X509Certificate2> GetPersistedSiteCertificateAsync();
		Task PersistAccountCertificateAsync(IKey certificate);
		Task PersistChallengesAsync(ChallengeDto[] challenges);
		Task PersistSiteCertificateAsync(byte[] certificateBytes);
		Task DeleteChallengesAsync(ChallengeDto[] challenges);
	}
}