using System;
using FluffySpoon.AspNet.LetsEncrypt.Persistence;
using Microsoft.Azure.Management.AppService.Fluent.Models;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;

namespace FluffySpoon.LetsEncrypt.Azure
{
	using System.Collections.Generic;
	using System.Security.Cryptography.X509Certificates;
	using System.Threading.Tasks;
	using FluffySpoon.AspNet.LetsEncrypt;
	using Microsoft.Azure.Management.AppService.Fluent;
	using Azure = Microsoft.Azure.Management.Fluent.Azure;

	public class AzureAppServiceSslBindingCertificatePersistenceStrategy : ICertificatePersistenceStrategy
	{
		private readonly AzureOptions options;
		private readonly IAzure client;

		private const string TagName = "FluffySpoonAspNetLetsEncrypt";

		public AzureAppServiceSslBindingCertificatePersistenceStrategy(
			AzureOptions azureOptions)
		{
			options = azureOptions;
			client = Authenticate();
		}

		private IAzure Authenticate()
		{
			return Azure.Authenticate(options.Credentials).WithDefaultSubscription();
		}

		public async Task PersistAsync(string key, byte[] bytes)
		{
			var azureCertificate = await GetExistingCertificateAsync(key);
			if (azureCertificate != null)
			{
				await client.WebApps.Manager
					.AppServiceCertificates
					.Inner
					.UpdateAsync(
						options.ResourceGroupName,
						azureCertificate.Name,
						new CertificatePatchResource()
						{
							Password = string.Empty,
							PfxBlob = bytes
						});
			}
			else
			{
				var certificate = new X509Certificate2(bytes);
				certificate.
				await client.WebApps.Manager
					.AppServiceCertificates
					.Define(TagName + "_" + Guid.NewGuid())
					.WithRegion("NorthEurope")
			}
		}

		public async Task<byte[]> RetrieveAsync(string key)
		{
			var certificate = await GetExistingCertificateAsync(key);
			return certificate?.PfxBlob;
		}

		private async Task<IAppServiceCertificate> GetExistingCertificateAsync(string key)
		{
			var certificates = await client.WebApps.Manager
				.AppServiceCertificates
				.ListByResourceGroupAsync(options.ResourceGroupName);

			foreach (var certificate in certificates)
			{
				if (certificate.FriendlyName != LetsEncryptRenewalService.CertificateFriendlyName)
					continue;

				var tags = certificate.Tags;
				if (!tags.ContainsKey(TagName) || tags[TagName] != key)
					continue;

				return certificate;
			}

			return null;
		}
	}
}
