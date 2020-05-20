using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FluffySpoon.AspNet.LetsEncrypt.Certes;
using FluffySpoon.AspNet.LetsEncrypt.Persistence;
using Microsoft.Azure.Management.AppService.Fluent;
using Microsoft.Azure.Management.AppService.Fluent.Models;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Extensions.Logging;

namespace FluffySpoon.AspNet.LetsEncrypt.Azure
{
    using Azure = Microsoft.Azure.Management.Fluent.Azure;

	public class AzureAppServiceSslBindingCertificatePersistenceStrategy : ICertificatePersistenceStrategy, IAzureAppServiceSslBindingCertificatePersistenceStrategy
	{
		private const string WildcardPrefix = "*.";
		private const string AzureCertThumbprintsAppSettingName = "WEBSITE_LOAD_CERTIFICATES";

		private readonly AzureOptions _azureOptions;
		private readonly LetsEncryptOptions _letsEncryptOptions;

		private readonly ILogger<IAzureAppServiceSslBindingCertificatePersistenceStrategy> _logger;
		private readonly IAzure _client;

		private string TagName
		{
			get
			{
				const string prefix = "FluffySpoonAspNetLetsEncrypt";

				var domainsTag = _letsEncryptOptions
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
			_azureOptions = azureOptions;
			_letsEncryptOptions = letsEncryptOptions;
			_logger = logger;

			_client = Authenticate();
		}

		private IAzure Authenticate()
		{
			return Azure.Authenticate(_azureOptions.Credentials).WithDefaultSubscription();
		}

		private bool DoesDomainMatch(string boundDomain, string certificateDomain) {
			if (certificateDomain.StartsWith(WildcardPrefix))
			{
				var regexPattern = ConstructWildcardDomainMatchingRegularExpression(certificateDomain);

				return Regex.IsMatch(boundDomain, regexPattern);
			}

			return certificateDomain.ToLower() == boundDomain.ToLower();
		}

		private string ConstructWildcardDomainMatchingRegularExpression(string wildcardDomain)
		{
			var regexPattern = wildcardDomain.Replace(@".", @"\.");
			regexPattern = Regex.Replace(regexPattern, @"^\*\\\.", @"^[^.]+\.");
			regexPattern = Regex.Replace(regexPattern, @"\.?$", @"\.?$");

			return regexPattern;
		}

		private bool DoesDomainMatch(string boundDomain, IEnumerable<string> certificateDomains)
		{
			return certificateDomains.Any(certDomain => DoesDomainMatch(boundDomain, certDomain));
		}

		private bool DoDomainsMatch(IEnumerable<string> boundDomains, IEnumerable<string> certificateDomains)
		{
			return boundDomains.Any(boundDomain => DoesDomainMatch(boundDomain, certificateDomains));
		}

		public async Task PersistAsync(CertificateType persistenceType, byte[] bytes)
		{
			if (bytes.Length == 0)
			{
				_logger.LogWarning("Tried to persist empty certificate.");
				return;
			}

			if (persistenceType != CertificateType.Site)
			{
				_logger.LogTrace("Skipping certificate persistence because a certificate of type {CertificateType} can't be persisted in Azure.", persistenceType);
				return;
			}

			var domains = _letsEncryptOptions.Domains.ToArray();

			_logger.LogInformation("Creating new Azure certificate of type {CertificateType} and domains {DomainNames}.", persistenceType, String.Join(", ", domains));

			var apps = await _client.WebApps.ListByResourceGroupAsync(_azureOptions.ResourceGroupName);

			var relevantApps = new HashSet<AzureAppInstance>();
			foreach (var app in apps)
			{
				_logger.LogTrace("Checking hostnames of app {AppName} (AppHostNames: {HostNames}) against domains {DomainNames}.", app.Name, app.HostNames, String.Join(", ", domains));

				if (DoDomainsMatch(app.HostNames, domains))
				{
					_logger.LogTrace("App {AppName} matches a domain", app.Name);

					relevantApps.Add(new AzureAppInstance(app));
				}

				if (app.DeploymentSlots != null)
				{
					var slots = await app.DeploymentSlots.ListAsync();

					foreach (var slot in slots)
					{
						_logger.LogTrace(
							"Checking hostnames of app {AppName}/slot {slot} (AppHostNames: {HostNames}) against domains {Domains}.",
							app.Name, slot.Name,
							slot.HostNames, String.Join(", ", domains));

						if (DoDomainsMatch(slot.HostNames, domains))
						{
							_logger.LogTrace("App {AppName}/slot {slot} matches a domain", app.Name, slot.Name);

							relevantApps.Add(new AzureAppInstance(app, slot));
						}
					}
				}
			}
			
			if (!relevantApps.Any())
				throw new InvalidOperationException(
					$"Could not find an app that has a hostname created for domains {String.Join(", ", domains)}.");

			var regionName = relevantApps.FirstOrDefault()?.RegionName;
		
			_logger.LogInformation("Found region name to use to use for new certificate: {RegionName}", regionName);

			IAppServiceCertificate newCertificate = await CreateOrUpdateCertificateAsync(bytes, regionName);

			foreach (var appTuple in relevantApps)
			{
				await UpdateSingleApp(appTuple, newCertificate, domains);
			}

			await DeleteOldCertificatesAsync(newCertificate);
		}

		/// <summary>
		/// If we've had to replace an existing certificate, then we'll need to delete the old one
		/// We actually delete any matching certificate created by FluffySpoon which ISN'T the one we
		/// just created
		/// </summary>
		/// <returns></returns>
		private async Task DeleteOldCertificatesAsync(IAppServiceCertificate newCertificate)
		{
			foreach (var certificate in await GetExistingAzureCertificatesAsync(CertificateType.Site))
			{
				if (certificate.Thumbprint != newCertificate.Thumbprint)
				{
					_logger.LogInformation("Deleting old Azure certificate {CertificateName}", certificate.Name);

					await _client.WebApps.Manager
						.AppServiceCertificates
						.Inner
						.DeleteAsync(
							_azureOptions.ResourceGroupName,
							certificate.Name);
				}
			}
		}

		private async Task<IAppServiceCertificate> CreateOrUpdateCertificateAsync(byte[] bytes, string regionName)
		{
			var azureCertificate = await GetExistingAzureCertificateAsync(CertificateType.Site);
			if (azureCertificate != null)
			{
				return await UpdateExistingCertificateAsync(azureCertificate, bytes);
			}
			else
			{
				return await CreateNewCertificateAsync(bytes, regionName);
			}
		}

		/// <summary>
		/// Update an existing certificate
		/// </summary>
		private async Task<IAppServiceCertificate> UpdateExistingCertificateAsync(IAppServiceCertificate existingCertificate, byte[] bytes)
		{
			_logger.LogInformation("Updating existing Azure certificate name {CertificateName}.", existingCertificate.Name);
			
			// Azure doesn't let us update a certificate which is bound (the api fails with 409 Conflict), and we can't
			// unbind the running certificate without causing problems for a running site
			// So we will need to create a new certificate, bind to that, and then delete the existing one
			return await CreateNewCertificateAsync(bytes, existingCertificate.RegionName);
		}

		/// <summary>
		/// Create a new certificate from scratch
		/// </summary>
		private async Task<IAppServiceCertificate> CreateNewCertificateAsync(byte[] bytes, string regionName)
		{
			var certificateName = TagName + "_" + Guid.NewGuid();
			
			_logger.LogInformation("Creating new Azure certificate with name {CertificateName}", certificateName);

			IAppServiceCertificate azureCertificate = await _client.WebApps.Manager
				.AppServiceCertificates
				.Define(certificateName)
				.WithRegion(regionName)
				.WithExistingResourceGroup(_azureOptions.ResourceGroupName)
				.WithPfxByteArray(bytes)
				.WithPfxPassword(nameof(FluffySpoon))
				.CreateAsync();

			_logger.LogTrace("Created new Azure certificate with name {CertificateName}", azureCertificate.Name);

			var tags = new Dictionary<string, string>();
			foreach (var tag in azureCertificate.Tags)
				tags.Add(tag.Key, tag.Value);

			tags.Add(TagName, GetTagValue(CertificateType.Site));

			_logger.LogInformation("Updating tags: {Tags} on certificate {CertificateName}", tags, azureCertificate.Name);

			await _client.WebApps.Manager
				.AppServiceCertificates
				.Inner
				.UpdateAsync(
					_azureOptions.ResourceGroupName,
					azureCertificate.Name,
					new CertificatePatchResource()
					{
						Tags = tags
					});
			return azureCertificate;
		}

		/// <summary>
		/// Update the bindings and the app settings for a single application instance 
		/// </summary>
		private async Task UpdateSingleApp(AzureAppInstance appInstance, 
			IAppServiceCertificate azureCertificate,
			string[] domains)
		{
			await UpdateAppBindingsAsync(appInstance, azureCertificate, domains);

			await UpdateAppSettingsAsync(appInstance, azureCertificate);
		}

		private async Task UpdateAppBindingsAsync(AzureAppInstance appInstance, 
			IAppServiceCertificate azureCertificate, 
			string[] domains)
		{
			_logger.LogInformation("Checking host name bindings for app {AppName}", appInstance.DisplayName);

			string[] domainsToUpgrade = appInstance.HostNames
				.Where(boundDomain => DoesDomainMatch(boundDomain, domains))
				.ToArray();

			foreach (var domain in domainsToUpgrade)
			{
				_logger.LogDebug("Checking host name binding for domain {DomainName}", domain);

				HostNameBindingInner existingBinding =
					await appInstance.GetHostNameBindingAsync(_client, _azureOptions.ResourceGroupName, domain);

				if (DoesBindingNeedUpdating(existingBinding, azureCertificate.Thumbprint))
				{
					_logger.LogDebug("Updating host name binding for app {AppName} domain {DomainName}",
						appInstance.DisplayName, domain);

					var newBinding = new HostNameBindingInner(
						azureResourceType: AzureResourceType.Website,
						hostNameType: HostNameType.Verified,
						customHostNameDnsRecordType: CustomHostNameDnsRecordType.CName,
						sslState: SslState.SniEnabled,
						thumbprint: azureCertificate.Thumbprint);

					await appInstance.SetHostNameBindingAsync(_client,
						_azureOptions.ResourceGroupName,
						domain,
						newBinding);
				}
			}
		}

		/// <summary>
		/// In order to be able to use the Windows certificate store mechanism for loading X509 certificates,
		/// we need to have the certificate thumbprint included in the WEBSITE_LOAD_CERTIFICATES environment variable
		///
		/// As it stands, there's possibly a problem with this code never removing thumbprints from this variable
		/// which may cause some kind of problem eventually.  Maybe we should prune thumbprints out of this
		/// if they correspond to certificates which are not loaded in Azure at all, though there may be
		/// all sorts of permission problems there...
		/// </summary>
		private async Task UpdateAppSettingsAsync(AzureAppInstance appInstance, IAppServiceCertificate azureCertificate)
		{
			_logger.LogDebug("Getting app settings for {Name}", appInstance.DisplayName);

			var appSettings = await appInstance.GetAppSettings(); 
		
			var loadCertificatesSetting = appSettings.ContainsKey(AzureCertThumbprintsAppSettingName)
				? appSettings[AzureCertThumbprintsAppSettingName].Value
				: String.Empty;
			var certThumbprintsToLoad =
				loadCertificatesSetting.Split(new [] {','}, StringSplitOptions.RemoveEmptyEntries).ToList();
			if (!certThumbprintsToLoad.Contains(azureCertificate.Thumbprint))
			{
				_logger.LogInformation("Adding certificate thumbprint {Thumbprint} to app setting name {AppSettingName}",
					azureCertificate.Thumbprint,
					AzureCertThumbprintsAppSettingName);

				certThumbprintsToLoad.Add(azureCertificate.Thumbprint);

				loadCertificatesSetting = String.Join(",", certThumbprintsToLoad);

				try
				{
					await appInstance.UpdateAppSettingAsync(AzureCertThumbprintsAppSettingName, loadCertificatesSetting);
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Error updating app settings for {Name}", appInstance.DisplayName);
				}
			}
		}
		
		private bool DoesBindingNeedUpdating(HostNameBindingInner existingBinding, string certificateThumbprint)
		{
			return existingBinding == null || existingBinding.SslState != SslState.SniEnabled || existingBinding.Thumbprint != certificateThumbprint;
		}

		public async Task<byte[]> RetrieveAsync(CertificateType certificateType)
		{
			var certificate = await GetExistingCertificateAsync(certificateType);

			if (certificate == null)
			{
				_logger.LogInformation("Certificate of type {CertificateType} not found.", certificateType);
				return null;
			}

			var pfxBlob = certificate.GetRawCertData();

			if (pfxBlob == null || pfxBlob.Length == 0)
			{
				_logger.LogError("Certificate was found (thumbprint {Thumbprint}), but PfxBlob was null or 0 length.", certificate.Thumbprint);
				return null;
			}

			return pfxBlob;
		}

		/// <summary>
		/// Get the existing certificate with the longest available lifetime, or null if we can't find one
		/// </summary>
		private async Task<IAppServiceCertificate> GetExistingAzureCertificateAsync(CertificateType certificateType)
		{
			return (await GetExistingAzureCertificatesAsync(certificateType)).OrderByDescending(cert => cert.ExpirationDate).FirstOrDefault();
		}
		
		private async Task<IEnumerable<IAppServiceCertificate>> GetExistingAzureCertificatesAsync(CertificateType certificateType)
		{
			var result = new List<IAppServiceCertificate>();
			
			if (certificateType != CertificateType.Site)
			{
				_logger.LogTrace("Skipping certificate retrieval of a certificate of type {CertificateType}, which can't be persisted in Azure.", certificateType);
				return result;
			}

			var certificates = await _client.WebApps.Manager
				.AppServiceCertificates
				.ListByResourceGroupAsync(_azureOptions.ResourceGroupName);

			var expectedTagValue = GetTagValue(certificateType);

			_logger.LogInformation("Trying to find existing Azure certificate of type {CertificateType} expected tag {TagName}:{ExpectedTagValue}.", certificateType, TagName, expectedTagValue);

			foreach (var certificate in certificates)
			{
				var tags = certificate.Tags;
				
				_logger.LogTrace("Considering Azure certificate name {CertificateName}, with tags {Tags}", certificate.Name, tags);

				if (!tags.ContainsKey(TagName) || tags[TagName] != expectedTagValue)
					continue;

				_logger.LogTrace("Matched Azure certificate name {CertificateName}", certificate.Name, tags);

				result.Add(certificate);
			}

			if (!result.Any())
			{
				_logger.LogWarning("Could not find any matching Azure certificates.");
			}

			return result;
		}


		private string GetTagValue(CertificateType certificateType)
		{
			if (_letsEncryptOptions.UseStaging)
				return $"{certificateType}-Staging";
			else
				return certificateType.ToString();
		}

		private async Task<X509Certificate2> GetExistingCertificateAsync(CertificateType persistenceType)
		{
			var azureCert = await GetExistingAzureCertificateAsync(persistenceType);

			if (azureCert == null)
				return null;
			
			_logger.LogTrace("Looking for X509 cert with thumbprint {Thumbprint}", azureCert.Thumbprint);


			var certStore = new X509Store(StoreName.My, StoreLocation.CurrentUser);
			certStore.Open(OpenFlags.ReadOnly);

			try
			{
				foreach (var cert in certStore.Certificates)
				{
					_logger.LogTrace("Considering X509 cert {Certificate}", cert);
				}

				var certCollection = certStore.Certificates.Find(
											X509FindType.FindByThumbprint,
											// Replace below with your certificate's thumbprint
											azureCert.Thumbprint,
											false);

				// Get the first cert with the thumbprint
				if (certCollection.Count > 0)
				{
					_logger.LogTrace("Found X509 cert with correct thumbprint {Thumbprint}", azureCert.Thumbprint);
					var cert = certCollection[0];
					return cert;
				}
			}
			finally
			{
				certStore.Close();
			}

			_logger.LogWarning("Could not load X509Certificate for existing Azure certificate thumbprint {Thumbprint} - does WEBSITE_LOAD_CERTIFICATES correctly contain this thumbprint?", azureCert.Thumbprint);

			return null;
		}
	}
}
