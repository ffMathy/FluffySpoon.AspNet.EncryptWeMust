using System;
using FluffySpoon.AspNet.LetsEncrypt.Persistence;
using Microsoft.Azure.Management.AppService.Fluent.Models;
using Microsoft.Azure.Management.Fluent;

namespace FluffySpoon.LetsEncrypt.Azure
{
	using System.Collections.Generic;
	using System.Security.Cryptography.X509Certificates;
	using System.Threading.Tasks;
	using FluffySpoon.AspNet.LetsEncrypt;
	using Microsoft.Azure.Management.AppService.Fluent;
	using Microsoft.Extensions.Logging;
	using Azure = Microsoft.Azure.Management.Fluent.Azure;

	public class AzureAppServiceSslBindingCertificatePersistenceStrategy : ICertificatePersistenceStrategy, IAzureAppServiceSslBindingCertificatePersistenceStrategy
	{
		private readonly AzureOptions options;
		private readonly ILogger<IAzureAppServiceSslBindingCertificatePersistenceStrategy> logger;
		private readonly IAzure client;

		private const string TagName = "FluffySpoonAspNetLetsEncrypt";

		public AzureAppServiceSslBindingCertificatePersistenceStrategy(
			AzureOptions azureOptions,
			ILogger<IAzureAppServiceSslBindingCertificatePersistenceStrategy> logger)
		{
			options = azureOptions;
			this.logger = logger;
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
				logger.LogInformation("Updating existing Azure certificate for key {0}.", key);
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
				var domain = certificate.GetNameInfo(X509NameType.DnsName, false);

				logger.LogInformation("Creating new Azure certificate for key {0} and domain {1}.", key, domain);

				var apps = await client.WebApps.ListByResourceGroupAsync(options.ResourceGroupName);

				string regionName = null;
				foreach(var app in apps) {
					if(!app.HostNames.Contains(domain))
						continue;

					regionName = app.RegionName;
					break;
				}

				if(regionName == null)
					throw new InvalidOperationException("Could not find an app that has a hostname created for domain " + domain + ".");

				logger.LogInformation("Found region name to use: {0}.", regionName);

				var certificateName = TagName + "_" + Guid.NewGuid();
				azureCertificate = await client.WebApps.Manager
					.AppServiceCertificates
					.Define(certificateName)
					.WithRegion(regionName)
					.WithExistingResourceGroup(options.ResourceGroupName)
					.WithPfxByteArray(bytes)
					.WithPfxPassword(string.Empty)
					.CreateAsync();

				var tags = new Dictionary<string, string>();
				foreach(var tag in azureCertificate.Tags)
					tags.Add(tag.Key, tag.Value);

				tags.Add(TagName, key);

				logger.LogInformation("Updating tags: {0}.", tags);

				await client.WebApps.Manager
					.AppServiceCertificates
					.Inner
					.UpdateAsync(
						options.ResourceGroupName,
						azureCertificate.Name,
						new CertificatePatchResource()
						{
							Tags = tags
						});

				foreach (var app in apps)
				{
					if (!app.HostNames.Contains(domain))
						continue;

					await client.WebApps.Inner.CreateOrUpdateHostNameBindingWithHttpMessagesAsync(
						options.ResourceGroupName,
						app.Name,
						domain,
						new HostNameBindingInner(
							azureResourceType: AzureResourceType.Website,
							hostNameType: HostNameType.Verified,
							customHostNameDnsRecordType: CustomHostNameDnsRecordType.CName,
							sslState: SslState.SniEnabled,
							thumbprint: azureCertificate.Thumbprint));
				}
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

			logger.LogInformation("Trying to find existing Azure certificate with key {0}.", key);

			foreach (var certificate in certificates)
			{
				var tags = certificate.Tags;
				if (!tags.ContainsKey(TagName) || tags[TagName] != key)
					continue;

				return certificate;
			}

			logger.LogInformation("Could not find existing Azure certificate.");

			return null;
		}
	}
}
