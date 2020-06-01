using FluffySpoon.AspNet.EncryptWeMust.Certes;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace FluffySpoon.AspNet.EncryptWeMust.EntityFramework.Sample
{
	public class Program
	{
		public const string DomainToUse = "ffmathyletsencrypt.ngrok.io";

		public static void Main(string[] args)
		{
			CreateWebHostBuilder(args).Build().Run();
		}

		public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
			WebHost.CreateDefaultBuilder(args)
				.ConfigureLogging(l => l.AddConsole(x => x.IncludeScopes = true))
				.UseKestrel()
				.UseUrls(
					"http://" + DomainToUse,
					"https://" + DomainToUse)
				.UseStartup<Startup>();
	}
}
