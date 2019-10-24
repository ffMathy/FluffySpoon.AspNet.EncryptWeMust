using FluffySpoon.AspNet.LetsEncrypt.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FluffySpoon.AspNet.LetsEncrypt.Aws
{
	public static class RegistrationExtensions
	{
		public static void AddFluffySpoonLetsEncryptAwsFunctionLogging(
			this IServiceCollection services,
			ILogger logger)
		{
			void AddGenericLogger<TContext>()
			{
				services.AddSingleton<ILogger<TContext>, GenericLoggerAdapter<TContext>>();
			}

			services.AddSingleton(logger);

			AddGenericLogger<IPersistenceService>();
			AddGenericLogger<ILetsEncryptRenewalService>();
			AddGenericLogger<IAwsDnsChallengePersistenceStrategy>();
		}

		public static void AddFluffySpoonLetsEncryptAwsRoute53DnsChallengePersistence(
			this IServiceCollection services,
			AwsOptions awsOptions)
		{
			services.AddFluffySpoonLetsEncryptDnsChallengePersistence(
				(provider) => new AwsDnsChallengePersistenceStrategy(
					awsOptions,
					provider.GetRequiredService<ILogger<IAwsDnsChallengePersistenceStrategy>>()));
		}
	}
}
