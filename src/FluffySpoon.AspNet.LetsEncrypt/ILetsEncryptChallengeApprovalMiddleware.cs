using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace FluffySpoon.AspNet.LetsEncrypt
{
	public interface ILetsEncryptChallengeApprovalMiddleware
	{
		Task InvokeAsync(HttpContext context);
	}
}