using FluffySpoon.AspNet.LetsEncrypt.Azure;
using FluffySpoon.AspNet.LetsEncrypt.Persistence;
using FluffySpoon.LetsEncrypt.Azure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FluffySpoon.AspNet.LetsEncrypt
{
	public static class RegistrationExtensions
	{
		public static void AddFluffySpoonLetsEncryptAzureFunctionLogging(
			this IServiceCollection services,
			ILogger logger)
		{
			void AddGenericLogger<TContext>()
			{
				services.AddSingleton<ILogger<TContext>, GenericLoggerAdapter<TContext>>();
			}

			services.AddSingleton(logger);

			AddGenericLogger<ILetsEncryptChallengeApprovalMiddleware>();
			AddGenericLogger<IPersistenceService>();
			AddGenericLogger<ILetsEncryptRenewalService>();
			AddGenericLogger<IAzureAppServiceSslBindingCertificatePersistenceStrategy>();
		}

		public static void AddFluffySpoonLetsEncryptAzureAppServiceSslBindingCertificatePersistence(
		this IServiceCollection services,
		AzureOptions azureOptions)
		{
			services.AddFluffySpoonLetsEncryptCertificatePersistence(
				(provider) => new AzureAppServiceSslBindingCertificatePersistenceStrategy(
					azureOptions,
					provider.GetRequiredService<LetsEncryptOptions>(),
					provider.GetRequiredService<ILogger<IAzureAppServiceSslBindingCertificatePersistenceStrategy>>()));
		}
	}
}
