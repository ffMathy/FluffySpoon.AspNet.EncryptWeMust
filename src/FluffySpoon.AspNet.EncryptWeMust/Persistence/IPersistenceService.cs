using System.Threading.Tasks;
using Certes;
using FluffySpoon.AspNet.EncryptWeMust.Certificates;

namespace FluffySpoon.AspNet.EncryptWeMust.Persistence
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