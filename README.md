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

_Note that you can set either `TimeAfterIssueDateBeforeRenewal`, `TimeAfterIssueDateBeforeRenewal` or both, but at least one of them has to be specified._

```csharp
services.AddFluffySpoonLetsEncrypt(new LetsEncryptOptions()
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

//the following line tells the middleware to persist the certificate to a file, so that if the server restarts, the certificate can be re-used without generating a new one.
services.AddFluffySpoonLetsEncryptFilePersistence();
```

## Inject the middleware
Inject the middleware in the `Startup` class' `Configure` method as such:

```csharp
public void Configure()
{
	app.UseFluffySpoonLetsEncrypt();
}
```

## Configure Kestrel to look for the certificate
Finally, to make Kestrel automatically select the LetsEncrypt certificate, we must configure it as such. Here's an example `Program.cs`:

```csharp
WebHost.CreateDefaultBuilder(args)
	.UseKestrel(kestrelOptions => kestrelOptions.ConfigureHttpsDefaults(
			httpsOptions => httpsOptions.ServerCertificateSelector = 
				(c, s) => LetsEncryptCertificateContainer.Instance.Certificate))
	.UseUrls("http://" + DomainToUse, "https://" + DomainToUse)
	.UseStartup<Startup>();
```

Tada! Your application now supports SSL via LetsEncrypt, even from the first HTTPS request. It will even renew your certificate automatically in the background.

# Configuring persistence
Persistence tells the middleware how to persist and retrieve the certificate, so that if the server restarts, the certificate can be re-used without generating a new one.

## File persistence
```csharp
//takes an optional file name parameter to specify which file to persist the certificate to.
services.AddFluffySpoonLetsEncryptFilePersistence();
```

## Custom persistence
```csharp
//takes an optional file name parameter to specify which file to persist the certificate to.
services.AddFluffySpoonLetsEncryptPersistence(/* your own ILetsEncryptCertificatePersistence implementation */);

//you can also customize persistence via delegates.
services.AddFluffySpoonLetsEncryptPersistence(
	async (bytes) => File.WriteAllBytes("myfile", bytes),
	async () => File.ReadAllBytes("myfile", bytes));
```

## Entity Framework persistence
Requires the NuGet package `FluffySpoon.AspNet.LetsEncrypt.EntityFramework`.

```csharp
// Certificate in this example is a database model class that has been configured with the database context.
class Certificate {
	public byte[] Bytes { get; set; }
}

//we only have to instruct how to add the certificate - `databaseContext.SaveChangesAsync()` is automatically called.
services.AddFluffySpoonLetsEncryptEntityFrameworkPersistence<DatabaseContext>(
	async (databaseContext, bytes) => databaseContext.Certificates.Add(new Certificate() { Bytes = bytes }),
	async (databaseContext) => databaseContext.Certificates.Single());
```

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
services.AddFluffySpoonRenewalLifecycleHook<MyLifecycleHook>();
```

# Really?
Yes, really. This even works in an Azure App Service - technically any host that can host ASP .NET Core 2.1 applications can use this without issues.

I got tired of disappointing Azure support for LetsEncrypt, which currently requires a plugin and potentially hours of fiddling around just to get it working.