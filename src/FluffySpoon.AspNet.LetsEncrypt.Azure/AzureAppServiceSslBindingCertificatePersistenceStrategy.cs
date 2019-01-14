using System;
using FluffySpoon.AspNet.LetsEncrypt.Persistence;
using Microsoft.Azure.Management.AppService.Fluent.Models;
using Microsoft.Azure.Management.Fluent;

namespace FluffySpoon.LetsEncrypt.Azure
{
	using System.Collections.Generic;
	using System.IO;
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

		public async Task PersistAsync(PersistenceType persistenceType, byte[] bytes)
		{
			if(bytes.Length == 0) {
				logger.LogWarning("Tried to persist empty certificate.");
				return;
			}

			if(persistenceType != PersistenceType.Site) {
				logger.LogTrace("Skipping certificate persistence because a certificate of type {0} can't be persisted in Azure.", persistenceType);
				return;
			}

			var azureCertificate = await GetExistingCertificateAsync(persistenceType);
			if (azureCertificate != null)
			{
				logger.LogInformation("Updating existing Azure certificate for key {0}.", persistenceType);
				await client.WebApps.Manager
					.AppServiceCertificates
					.Inner
					.UpdateAsync(
						options.ResourceGroupName,
						azureCertificate.Name,
						new CertificatePatchResource()
						{
							Password = nameof(FluffySpoon),
							PfxBlob = bytes
						});
			}
			else
			{
				logger.LogDebug("Will create new Azure certificate for key {0} of {1} bytes", persistenceType, bytes.Length);

				var certificate = new X509Certificate2(bytes, nameof(FluffySpoon));
				var certificateWithPasswordBytes = certificate.Export(X509ContentType.Pfx, nameof(FluffySpoon));
				
				var domain = certificate.GetNameInfo(X509NameType.DnsName, false);

				logger.LogInformation("Creating new Azure certificate for key {0} and domain {1}.", persistenceType, domain);

				var apps = await client.WebApps.ListByResourceGroupAsync(options.ResourceGroupName);

				string regionName = null;

				var relevantApps = new HashSet<IWebApp>();
				foreach(var app in apps)
				{
					logger.LogTrace("Checking hostnames of app {0} ({1}) against domain {2}.", app.Name, app.HostNames, domain);

					if (!app.HostNames.Contains(domain))
						continue;

					regionName = app.RegionName;
					relevantApps.Add(app);
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
					.WithPfxByteArray(certificateWithPasswordBytes)
					.WithPfxPassword(nameof(FluffySpoon))
					.CreateAsync();

				var tags = new Dictionary<string, string>();
				foreach(var tag in azureCertificate.Tags)
					tags.Add(tag.Key, tag.Value);

				tags.Add(TagName, persistenceType.ToString());

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

				foreach (var app in relevantApps)
				{
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

		public async Task<byte[]> RetrieveAsync(PersistenceType persistenceType)
		{
			var certificate = await GetExistingCertificateAsync(persistenceType);
			return certificate?.PfxBlob;
		}

		private async Task<IAppServiceCertificate> GetExistingCertificateAsync(PersistenceType persistenceType)
		{
			if (persistenceType != PersistenceType.Site)
			{
				logger.LogTrace("Skipping certificate retrieval of a certificate of type {0}, which can't be persisted in Azure.", persistenceType);
				return null;
			}

			var certificates = await client.WebApps.Manager
				.AppServiceCertificates
				.ListByResourceGroupAsync(options.ResourceGroupName);

			logger.LogInformation("Trying to find existing Azure certificate with key {0}.", persistenceType);

			foreach (var certificate in certificates)
			{
				var tags = certificate.Tags;
				if (!tags.ContainsKey(TagName) || tags[TagName] != persistenceType.ToString())
					continue;

				return certificate;
			}

			logger.LogInformation("Could not find existing Azure certificate.");

			return null;
		}
	}
}
