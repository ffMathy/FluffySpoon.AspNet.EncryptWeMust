using FluffySpoon.AspNet.EncryptWeMust.Azure;
using FluffySpoon.AspNet.EncryptWeMust.Certes;
using FluffySpoon.AspNet.EncryptWeMust.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// ReSharper disable once CheckNamespace
namespace FluffySpoon.AspNet.EncryptWeMust
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
