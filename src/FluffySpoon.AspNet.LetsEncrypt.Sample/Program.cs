using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Certes;
using Certes.Acme;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Https.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.X509;

namespace FluffySpoon.AspNet.LetsEncrypt.Sample
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
				.UseKestrel(kestrelOptions =>
				{
					kestrelOptions.ConfigureHttpsDefaults(httpsOptions =>
					{
						httpsOptions.ServerCertificateSelector = (c, s) => LetsEncryptCertificateContainer.Instance.Certificate;
					});
				})
				.UseUrls(
					"http://" + DomainToUse,
					"https://" + DomainToUse)
				.UseStartup<Startup>();
	}
}
