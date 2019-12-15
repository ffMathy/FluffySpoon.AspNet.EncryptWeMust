using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace FluffySpoon.AspNet.LetsEncrypt.Certes
{
	public interface ILetsEncryptRenewalService: IHostedService, IDisposable
	{
		Uri LetsEncryptUri { get; }
		Task RunOnceAsync();
	}
}