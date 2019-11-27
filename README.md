The simplest LetsEncrypt setup for ASP .NET Core. Almost no server configuration needed. 

`Install-Package FluffySpoon.AspNet.LetsEncrypt`
 
# Requirements
- Kestrel (which is default)
- ASP .NET Core 2.1+
- An always-on app-pool

## Getting an always-on app pool
This is required because the renewal job runs on a background thread and polls once every hour to see if the certificate needs renewal (this is a very cheap operation). 

It can be enabled using __just one__ the following techniques:
- Enabling Always On if using Azure App Service.
- Setting `StartMode` of the app pool to `AlwaysRunning` if using IIS.
- Hosting your ASP .NET Core application as a Windows Service.

# Usage example
If you want to try it yourself, you can also browse the sample project code here:

https://github.com/ffMathy/FluffySpoon.AspNet.LetsEncrypt/tree/master/src/FluffySpoon.AspNet.LetsEncrypt.Sample

## Configure the services
Add the following code to your `Startup` class' `ConfigureServices` method with real values instead of the sample values:

_Note that you can set either `TimeUntilExpiryBeforeRenewal`, `TimeAfterIssueDateBeforeRenewal` or both, but at least one of them has to be specified._

```csharp
//the following line adds the automatic renewal service.
services.AddFluffySpoonLetsEncryptRenewalService(new LetsEncryptOptions()
{
	Email = "some-email@github.com", //LetsEncrypt will send you an e-mail here when the certificate is about to expire
	UseStaging = false, //switch to true for testing
	Domains = new[] { DomainToUse },
	TimeUntilExpiryBeforeRenewal = TimeSpan.FromDays(30), //renew automatically 30 days before expiry
	TimeAfterIssueDateBeforeRenewal = TimeSpan.FromDays(7), //renew automatically 7 days after the last certificate was issued
	CertificateSigningRequest = new CsrInfo() //these are your certificate details
	{
		CountryName = "Denmark",
		Locality = "DK",
		Organization = "Fluffy Spoon",
		OrganizationUnit = "Hat department",
		State = "DK"
	}
});

//the following line tells the library to persist the certificate to a file, so that if the server restarts, the certificate can be re-used without generating a new one.
services.AddFluffySpoonLetsEncryptFileCertificatePersistence();

//the following line tells the library to persist challenges in-memory. challenges are the "/.well-known" URL codes that LetsEncrypt will call.
services.AddFluffySpoonLetsEncryptMemoryChallengePersistence();
```

## Inject the middleware
Inject the middleware in the `Startup` class' `Configure` method as such:

```csharp
public void Configure()
{
	app.UseFluffySpoonLetsEncryptChallengeApprovalMiddleware();
}
```

## Configure Kestrel to look for the certificate
Finally, to make Kestrel automatically select the LetsEncrypt certificate, we must configure it as such. Here's an example `Program.cs`:

```csharp
WebHost.CreateDefaultBuilder(args)
	.UseKestrel(kestrelOptions => kestrelOptions.ConfigureHttpsDefaults(
			httpsOptions => httpsOptions.ServerCertificateSelector = 
				(c, s) => LetsEncryptRenewalService.Certificate))
	.UseUrls("http://" + DomainToUse, "https://" + DomainToUse)
	.UseStartup<Startup>();
```

Tada! Your application now supports SSL via LetsEncrypt, even from the first HTTPS request. It will even renew your certificate automatically in the background.

# Optional: Configuring persistence
Persistence tells the middleware how to persist and retrieve the certificate, so that if the server restarts, the certificate can be re-used without generating a new one.

A certificate has a _key_ to distinguish between certificates, since there is both an account certificate and a site certificate that needs to be stored.

## File persistence
```csharp
services.AddFluffySpoonLetsEncryptFileCertificatePersistence();
services.AddFluffySpoonLetsEncryptFileChallengePersistence();
```

## Custom persistence
```csharp
services.AddFluffySpoonLetsEncryptCertificatePersistence(/* your own ILetsEncryptPersistence implementation */);
services.AddFluffySpoonLetsEncryptChallengePersistence(/* your own ILetsEncryptPersistence implementation */);

//you can also customize persistence via delegates.
services.AddFluffySpoonLetsEncryptCertificatePersistence(
	async (key, bytes) => File.WriteAllBytes("certificate_" + key, bytes),
	async (key) => File.ReadAllBytes("certificate_" + key, bytes));

//the same can be done for challenges, with different arguments.
services.AddFluffySpoonLetsEncryptChallengePersistence(
	async (challenges) => ... /* Do something to serialize the collection of challenges and store it */,
	async () => ... /* Retrieve the stored collection of challenges */,
	async (challenges) => ... /* Delete the specified challenges */);
```

## Entity Framework persistence
Requires the NuGet package `FluffySpoon.AspNet.LetsEncrypt.EntityFramework`.

```csharp
// Certificate and Challenge in this example are database model classes that have been configured with the database context.
class Certificate {
	[Key]
	public string Key { get; set; }
	public byte[] Bytes { get; set; }
}

public class Challenge
{
	[Key]
	public string Token { get; set; }
	public string Response { get; set; }
	public int Type { get; set; }
	public string Domains { get; set; }
}

//we only have to instruct how to add the certificate - `databaseContext.SaveChangesAsync()` is automatically called.
services.AddFluffySpoonLetsEncryptEntityFrameworkCertificatePersistence<DatabaseContext>(
	async (databaseContext, key, bytes) =>
	{
		var existingCertificate = databaseContext.Certificates.SingleOrDefault(x => x.Key == key);
		if (existingCertificate != null)
		{
			existingCertificate.Bytes = bytes;
		}
		else
		{
			databaseContext.Certificates.Add(new Certificate()
			{
				Key = key,
				Bytes = bytes
			});
		}
	},
	async (databaseContext, key) => databaseContext
		.Certificates
		.SingleOrDefault(x => x.Key == key)
		?.Bytes);

//the same can be done for challenges
services.AddFluffySpoonLetsEncryptEntityFrameworkChallengePersistence<DatabaseContext>(
	async (databaseContext, challenges) => databaseContext
		.Challenges
		.AddRange(
			challenges.Select(x =>
				new Challenge()
				{
					Token = x.Token,
					Response = x.Response,
					Type = (int)x.Type,
					Domains = String.Join(",", x.Domains)
				})),
	async (databaseContext) => databaseContext
		.Challenges
		.Select(x =>
			new ChallengeDto()
			{
				Token = x.Token,
				Response = x.Response,
				Type = (ChallengeType)x.Type,
				Domains = x.Domains.Split(',', StringSplitOptions.RemoveEmptyEntries)
			}),
	async (databaseContext, challenges) => databaseContext
		.Challenges
		.RemoveRange(
			databaseContext
				.Challenges
				.Where(x => challenges.Any(y => y.Token == x.Token))
			));
```

## Distributed cache (Redis etc) persistence
Requires:
- The NuGet package `FluffySpoon.AspNet.LetsEncrypt.DistributedCache`.
- A configured distributed cache in ASP .NET Core using the `services.AddDistributedRedisCache()` or similar.

```csharp
services.AddFluffySpoonLetsEncryptDistributedCertificatePersistence(expiry: TimeSpan.FromDays(30));
services.AddFluffySpoonLetsEncryptDistributedChallengePersistence(expiry: TimeSpan.FromHours(1));
```

# Azure App Service

Using this project when running as an Azure App Service requires a few things.

Firstly the App Service Plan needs to have the "Custom domains / SSL" feature (currently B1 for testing, S1 for production are the lowest supported).

Secondly you should use the `AzureAppServiceSslBindingCertificatePersistenceStrategy` strategy:

```csharp
services.AddFluffySpoonLetsEncryptAzureAppServiceSslBindingCertificatePersistence(
  new AzureOptions {
    ResourceGroupName = ..., // The resource group the App Service is deployed to
    Credentials = ... // Get some credentials that have access to Azure
  });
```

The credentials supplied above need to have access to create certificates and set SSL bindings for the App Service. The permissions to create certificates is for the resource group and they are created as resources in the group, not in the App Service itself. The SSL bindings are set on the App Service. Bottom line is that you can achieve this by granting the [Website Contributor](https://docs.microsoft.com/en-us/azure/role-based-access-control/built-in-roles#website-contributor) to whatever principal you wish to use (the credentials in the snippet above).

The easiest way to get some usable credentials is to use a [System Assigned Managed Identity](https://docs.microsoft.com/en-us/azure/active-directory/managed-identities-azure-resources/overview). This can be enabled on an App Service as described [here](https://docs.microsoft.com/en-us/azure/app-service/overview-managed-identity).

Having done that the following snippet sets this up:

```csharp
var managedIdentityCredentials = new AzureCredentialsFactory()
  .FromMSI(
     new MSILoginInformation(MSIResourceType.AppService),
     AzureEnvironment.AzureGlobalCloud);

services.AddFluffySpoonLetsEncryptAzureAppServiceSslBindingCertificatePersistence(
  new AzureOptions {
    ResourceGroupName = System.Environment.GetEnvironmentVariable("WEBSITE_RESOURCE_GROUP"),
    Credentials = managedIdentityCredentials
  });
```

The resource group for the App Service can also easily be accessed through an environment variable, as specified above.

# DNS challenges & wildcard certificates

By default, challenges are validated over HTTP. This requires that the domain being validated resolves to an operational web server which can respond with the correct token. There are some rare circumstances where this may not be possible. Additionally, this method does not support wildcard certificates.

The use of DNS validation can overcome these issues. The `FluffySpoon.AspNet.LetsEncrypt.Aws` package includes an implementation of DNS challenge support using AWS Route 53.

DNS support is not enabled by default. To enable DNS challenge support, you must register at least one DNS-enabled challenge persistence strategy. The example below shows how to add DNS challenge support using the Route 53 implementation:

```csharp
services.AddFluffySpoonLetsEncryptAwsRoute53DnsChallengePersistence(
	new AwsOptions()
	{
		Region = ..., // Specify the AWS Region
		Credentials = new BasicAWSCredentials(...) // Specify some IAM credentials with access to read and write to Route 53
	});
```

DNS validation can (and in cases where you're requesting any non-wildcard domains, must) be used in conjunction with HTTP validation - Lets Encrypt will try to use both if possible. It can be used in conjunction with other persistence strategies, including Azure.

Wildcard certificates can be requested by supplying a wildcard domain in the list of domains to include in the certificate order e.g. `*.mydomain.com`.

## Custom DNS persistence

To implement support for other DNS providers, derive from `BaseDnsChallengePersistenceStrategy` and implement the methods `PersistAsync` and `DeleteAsync`, then register it using the `AddFluffySpoonLetsEncryptChallengePersistence` services configuration extension. 

```csharp
services.AddFluffySpoonLetsEncryptChallengePersistence(
	(provider) => new MyDnsChallengePersistenceStrategy(...));
```

### PersistAsync

This method persists a challenge record to DNS. The fully qualified name is supplied in `recordName`, the type of record in `recordType` (this is currently always "TXT"), and the value of the record in `recordValue`. Note that some DNS providers will require that TXT values are enclosed with the `"` double quote character. Your implementation is responsible for applying this if necessary.

If your DNS provider supports it, you should allow for multiple values to be present for any given record. The PersistAsync method should add the value to the existing list of values if the record is already present.

### DeleteAsync

Deletion is requested after validation of the certificate has completed. It is highly recommended to implement the deletion of the specified DNS record value, to avoid accumulation of out-of-date challenge tokens.

If your DNS provider supports it, you should allow for multiple values to be present for any given record. The DeleteAsync method should only remove the specified value from the list of values if there is more than one. If the value is the only one, then the whole record can be deleted.

# Hooking into events
You can register a an `ICertificateRenewalLifecycleHook` implementation which does something when certain events occur, as shown below. This can be useful if you need to notify a Slack channel or send an e-mail if an error occurs, or when the certificate has indeed been renewed.

```csharp
class MyLifecycleHook : ICertificateRenewalLifecycleHook {
	public async Task OnStartAsync() {
		//when the renewal background job has started.
	}

	public async Task OnStopAsync() {
		//when the renewal background job (or the application) has stopped.
		//this is not guaranteed to fire in critical application crash scenarios.
	}

	public async Task OnRenewalSucceededAsync() {
		//when the renewal has completed.
	}

	public async Task OnExceptionAsync(Exception error) {
		//when an error happened during the renewal process.
	}
}

//this is how to wire up the hook.
services.AddFluffySpoonLetsEncryptRenewalLifecycleHook<MyLifecycleHook>();
```
