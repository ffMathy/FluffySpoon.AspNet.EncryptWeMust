using System.Threading.Tasks;

namespace FluffySpoon.AspNet.LetsEncrypt.Aws
{
	public interface IAwsDnsChallengePersistenceStrategy
	{
		Task DeleteAsync(string recordName, string recordType);
		Task PersistAsync(string recordName, string recordType, string recordValue);
	}
}