using System;
using System.Threading.Tasks;

namespace FluffySpoon.AspNet.LetsEncrypt
{
	public interface ICertificateRenewalLifecycleHook
	{
		Task OnStartAsync();
		Task OnStopAsync();
		Task OnRenewalSucceededAsync();
		Task OnExceptionAsync(Exception error);
	}
}