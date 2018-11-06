using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace FluffySpoon.AspNet.LetsEncrypt
{
	public interface ILetsEncryptChallengeApprovalMiddleware1
	{
		Task InvokeAsync(HttpContext context);
	}
}