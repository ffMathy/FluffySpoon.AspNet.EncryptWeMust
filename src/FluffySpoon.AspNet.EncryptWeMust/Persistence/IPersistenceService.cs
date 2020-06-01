using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Certes;
using FluffySpoon.AspNet.LetsEncrypt.Certificates;

namespace FluffySpoon.AspNet.LetsEncrypt.Persistence
{
	public interface IPersistenceService
	{
		Task<IKey> GetPersistedAccountCertificateAsync();
		Task<ChallengeDto[]> GetPersistedChallengesAsync();
		Task<IAbstractCertificate> GetPersistedSiteCertificateAsync();
		Task PersistAccountCertificateAsync(IKey certificate);
		Task PersistChallengesAsync(ChallengeDto[] challenges);
		Task PersistSiteCertificateAsync(IPersistableCertificate certificate);
		Task DeleteChallengesAsync(ChallengeDto[] challenges);
	}
}