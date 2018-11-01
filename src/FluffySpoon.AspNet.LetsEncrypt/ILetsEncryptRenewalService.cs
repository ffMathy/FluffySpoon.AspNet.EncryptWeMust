using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace FluffySpoon.AspNet.LetsEncrypt
{
	public interface ILetsEncryptRenewalService: IHostedService, IDisposable
	{
		Uri LetsEncryptUri { get; }
		Task RunOnceAsync();
	}
}