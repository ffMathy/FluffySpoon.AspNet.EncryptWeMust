using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace FluffySpoon.AspNet.LetsEncrypt.Certes
{
	public interface ILetsEncryptChallengeApprovalMiddleware
	{
		Task InvokeAsync(HttpContext context);
	}
}