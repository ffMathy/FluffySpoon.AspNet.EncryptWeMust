using System;
using FluffySpoon.AspNet.LetsEncrypt.Persistence;
using Microsoft.Azure.Management.AppService.Fluent.Models;
using Microsoft.Azure.Management.Fluent;

namespace FluffySpoon.LetsEncrypt.Azure
{
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Security.Cryptography.X509Certificates;
	using System.Text.RegularExpressions;
	using System.Threading.Tasks;
	using FluffySpoon.AspNet.LetsEncrypt;
	using Microsoft.Azure.Management.AppService.Fluent;
	using Microsoft.Extensions.Logging;
	using Azure = Microsoft.Azure.Management.Fluent.Azure;

	public class AzureAppServiceSslBindingCertificatePersistenceStrategy : ICertificatePersistenceStrategy, IAzureAppServiceSslBindingCertificatePersistenceStrategy
	{
		private const string WildcardPrefix = "*.";
		private const string AzureCertThumbprintsAppSettingName = "WEBSITE_LOAD_CERTIFICATES";

		private readonly AzureOptions azureOptions;
		private readonly LetsEncryptOptions letsEncryptOptions;

		private readonly ILogger<IAzureAppServiceSslBindingCertificatePersistenceStrategy> logger;
		private readonly IAzure client;

		private string TagName
		{
			get
			{
				const string prefix = "FluffySpoonAspNetLetsEncrypt";

				var domainsTag = letsEncryptOptions
					.Domains
					.Aggregate(string.Empty, (a, b) => a + "," + b);
				return prefix + "_" + domainsTag;
			}
		}

		public AzureAppServiceSslBindingCertificatePersistenceStrategy(
			AzureOptions azureOptions,
			LetsEncryptOptions letsEncryptOptions,
			ILogger<IAzureAppServiceSslBindingCertificatePersistenceStrategy> logger)
		{
			this.azureOptions = azureOptions;
			this.letsEncryptOptions = letsEncryptOptions;
			this.logger = logger;

			client = Authenticate();
		}

		private IAzure Authenticate()
		{
			return Azure.Authenticate(azureOptions.Credentials).WithDefaultSubscription();
		}

		private bool DomainMatches(string certificateDomain, string boundDomain) {
			if (certificateDomain.StartsWith(WildcardPrefix))
			{
				var regexPattern = certificateDomain.Replace(@".", @"\.");
				regexPattern = Regex.Replace(regexPattern, @"^\*\\\.", @"^[^.]+\.");

				return Regex.IsMatch(boundDomain, regexPattern);
			}

			return certificateDomain.ToLower() == boundDomain.ToLower();
		}

		public async Task PersistAsync(PersistenceType persistenceType, byte[] bytes)
		{
			if (bytes.Length == 0)
			{
				logger.LogWarning("Tried to persist empty certificate.");
				return;
			}

			if (persistenceType != PersistenceType.Site)
			{
				logger.LogTrace("Skipping certificate persistence because a certificate of type {0} can't be persisted in Azure.", persistenceType);
				return;
			}

			var domains = letsEncryptOptions.Domains.ToArray();

			logger.LogInformation("Creating new Azure certificate for key {0} and domains {1}.", persistenceType, String.Join(", ", domains));

			var apps = await client.WebApps.ListByResourceGroupAsync(azureOptions.ResourceGroupName);

			string regionName = null;

			var relevantApps = new HashSet<(IWebApp App, IDeploymentSlot Slot)>();
			foreach (var app in apps)
			{
				logger.LogTrace("Checking hostnames of app {0} ({1}) against domains {2}.", app.Name, app.HostNames, String.Join(", ", domains));

				if (azureOptions.Slot == null)
				{
					if (!app.HostNames.Any(boundDomain => domains.Any(certDomain => DomainMatches(certDomain, boundDomain))))
						continue;

					relevantApps.Add((app, null));
				}
				else
				{
					var slots = app.DeploymentSlots
						.List()
						.Where(x => x
							.HostNames
							.Any(boundDomain => domains.Any(certDomain => DomainMatches(certDomain, boundDomain))));
					if (!slots.Any())
						continue;

					foreach (var slot in slots)
						relevantApps.Add((app, slot));
				}

				regionName = app.RegionName;
			}

			if (regionName == null)
				throw new InvalidOperationException("Could not find an app that has a hostname created for domains " + String.Join(", ", domains) + ".");

			var azureCertificate = await GetExistingAzureCertificateAsync(persistenceType);
			if (azureCertificate != null)
			{
				logger.LogInformation("Updating existing Azure certificate for key {0}.", persistenceType);
				await client.WebApps.Manager
					.AppServiceCertificates
					.Inner
					.UpdateAsync(
						azureOptions.ResourceGroupName,
						azureCertificate.Name,
						new CertificatePatchResource()
						{
							Password = nameof(FluffySpoon),
							HostNames = domains,
							PfxBlob = bytes
						});
				azureCertificate = await GetExistingAzureCertificateAsync(persistenceType);
			}
			else
			{
				logger.LogInformation("Found region name to use: {0}.", regionName);

				var certificateName = TagName + "_" + Guid.NewGuid();
				azureCertificate = await client.WebApps.Manager
					.AppServiceCertificates
					.Define(certificateName)
					.WithRegion(regionName)
					.WithExistingResourceGroup(azureOptions.ResourceGroupName)
					.WithPfxByteArray(bytes)
					.WithPfxPassword(nameof(FluffySpoon))
					.CreateAsync();

				var tags = new Dictionary<string, string>();
				foreach (var tag in azureCertificate.Tags)
					tags.Add(tag.Key, tag.Value);

				tags.Add(TagName, persistenceType.ToString());

				logger.LogInformation("Updating tags: {0}.", tags);

				await client.WebApps.Manager
					.AppServiceCertificates
					.Inner
					.UpdateAsync(
						azureOptions.ResourceGroupName,
						azureCertificate.Name,
						new CertificatePatchResource()
						{
							Tags = tags
						});
			}

			foreach (var appTuple in relevantApps)
			{
				string[] domainsToUpgrade;
				if (azureOptions.Slot == null)
				{
					logger.LogInformation("Checking host name bindings for app {0}", appTuple.App.Name);
					domainsToUpgrade = appTuple
						.App
						.HostNames
						.Where(boundDomain => domains.Any(certDomain => DomainMatches(certDomain, boundDomain)))
						.ToArray();
				}
				else
				{
					logger.LogInformation("Checking host name bindings for app {0}/{1}", appTuple.App.Name, appTuple.Slot.Name);
					domainsToUpgrade = appTuple
						.Slot
						.HostNames
						.Where(boundDomain => domains.Any(certDomain => DomainMatches(certDomain, boundDomain)))
						.ToArray();
				}

				foreach (var domain in domainsToUpgrade)
				{
					logger.LogDebug("Checking host name binding for domain {0}", domain);

					if (azureOptions.Slot == null)
					{
						var existingBinding = await client.WebApps.Inner.GetHostNameBindingAsync(azureOptions.ResourceGroupName,
							appTuple.App.Name,
							domain);

						if (DoesBindingNeedUpdating(existingBinding, azureCertificate.Thumbprint))
						{
							logger.LogDebug("Updating host name binding for domain {0}", domain);

							await client.WebApps.Inner.CreateOrUpdateHostNameBindingWithHttpMessagesAsync(
								azureOptions.ResourceGroupName,
								appTuple.App.Name,
								domain,
								new HostNameBindingInner(
									azureResourceType: AzureResourceType.Website,
									hostNameType: HostNameType.Verified,
									customHostNameDnsRecordType: CustomHostNameDnsRecordType.CName,
									sslState: SslState.SniEnabled,
									thumbprint: azureCertificate.Thumbprint));
						}
					}
					else
					{
						var existingBinding = await client.WebApps.Inner.GetHostNameBindingSlotAsync(azureOptions.ResourceGroupName,
							appTuple.App.Name,
							appTuple.Slot.Name,
							domain);

						if (DoesBindingNeedUpdating(existingBinding, azureCertificate.Thumbprint))
						{
							logger.LogDebug("Updating host name binding for domain {0}", domain);

							await client.WebApps.Inner.CreateOrUpdateHostNameBindingSlotWithHttpMessagesAsync(
							azureOptions.ResourceGroupName,
							appTuple.App.Name,
							domain,
							new HostNameBindingInner(
								azureResourceType: AzureResourceType.Website,
								hostNameType: HostNameType.Verified,
								customHostNameDnsRecordType: CustomHostNameDnsRecordType.CName,
								sslState: SslState.SniEnabled,
								thumbprint: azureCertificate.Thumbprint),
							appTuple.Slot.Name);
						}
					}
				}

				logger.LogDebug("Getting app settings");

				var appSettings = await client.WebApps.Manager
					.WebApps
					.GetByResourceGroup(appTuple.App.ResourceGroupName, appTuple.App.Name)
					.GetAppSettingsAsync();

				var loadCertificatesSetting = appSettings.ContainsKey(AzureCertThumbprintsAppSettingName) ? appSettings[AzureCertThumbprintsAppSettingName].Value : String.Empty;
				var certThumbprintsToLoad = loadCertificatesSetting.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();
				if (!certThumbprintsToLoad.Contains(azureCertificate.Thumbprint))
				{
					logger.LogInformation("Adding certificate thumbprint {0} to {1} app setting", azureCertificate.Thumbprint, AzureCertThumbprintsAppSettingName);

					certThumbprintsToLoad.Add(azureCertificate.Thumbprint);

					loadCertificatesSetting = String.Join(",", certThumbprintsToLoad);

					try
					{
						await client.WebApps.Manager
							.WebApps
							.GetByResourceGroup(appTuple.App.ResourceGroupName, appTuple.App.Name)
							.Update()
							.WithAppSetting(AzureCertThumbprintsAppSettingName, loadCertificatesSetting)
							.ApplyAsync();
					}
					catch (Exception ex)
					{
						logger.LogError(ex, "Error updating app settings for {0}", appTuple.App.Name);
					}
				}
			}
		}

		private bool DoesBindingNeedUpdating(HostNameBindingInner existingBinding, string certificateThumbprint)
		{
			return existingBinding == null || existingBinding.SslState != SslState.SniEnabled || existingBinding.Thumbprint != certificateThumbprint;
		}

		public async Task<byte[]> RetrieveAsync(PersistenceType persistenceType)
		{
			var certificate = await GetExistingCertificateAsync(persistenceType);

			if (certificate == null)
			{
				logger.LogInformation("Certificate of type {0} not found.", persistenceType);
				return null;
			}

			var pfxBlob = certificate?.GetRawCertData();

			if (pfxBlob == null || pfxBlob.Length == 0)
			{
				logger.LogError("Certificate was found (thumbprint {0}), but PfxBlob was null or 0 length.", certificate.Thumbprint);
				return null;
			}

			return pfxBlob;
		}

		private async Task<IAppServiceCertificate> GetExistingAzureCertificateAsync(PersistenceType persistenceType)
		{
			if (persistenceType != PersistenceType.Site)
			{
				logger.LogTrace("Skipping certificate retrieval of a certificate of type {0}, which can't be persisted in Azure.", persistenceType);
				return null;
			}

			var certificates = await client.WebApps.Manager
				.AppServiceCertificates
				.ListByResourceGroupAsync(azureOptions.ResourceGroupName);

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

		private async Task<X509Certificate2> GetExistingCertificateAsync(PersistenceType persistenceType)
		{
			var azureCert = await GetExistingAzureCertificateAsync(persistenceType);

			if (azureCert == null)
				return null;

			var certStore = new X509Store(StoreName.My, StoreLocation.CurrentUser);
			certStore.Open(OpenFlags.ReadOnly);

			try
			{
				var certCollection = certStore.Certificates.Find(
											X509FindType.FindByThumbprint,
											// Replace below with your certificate's thumbprint
											azureCert.Thumbprint,
											false);

				// Get the first cert with the thumbprint
				if (certCollection.Count > 0)
				{
					var cert = certCollection[0];
					return cert;
				}
			}
			finally
			{
				certStore.Close();
			}

			logger.LogInformation("Could not find existing Azure certificate.");

			return null;
		}
	}
}
