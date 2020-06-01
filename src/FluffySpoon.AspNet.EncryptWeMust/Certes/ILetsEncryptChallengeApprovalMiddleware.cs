using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace FluffySpoon.AspNet.EncryptWeMust.Certes
{
	public interface ILetsEncryptChallengeApprovalMiddleware
	{
		Task InvokeAsync(HttpContext context);
	}
}