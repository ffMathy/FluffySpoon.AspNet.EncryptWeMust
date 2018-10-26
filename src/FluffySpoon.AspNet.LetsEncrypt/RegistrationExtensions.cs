using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace FluffySpoon.AspNet.LetsEncrypt
{
	public static class RegistrationExtensions
	{
		public static void AddFluffySpoonRenewalLifecycleHook<TCertificateRenewalLifecycleHook>(
			this IServiceCollection services) where TCertificateRenewalLifecycleHook : class, ICertificateRenewalLifecycleHook
		{
			services.AddSingleton<ICertificateRenewalLifecycleHook, TCertificateRenewalLifecycleHook>();
		}

		public static void AddFluffySpoonLetsEncryptPersistence(
			this IServiceCollection services,
			Func<byte[], Task> persistAsync,
			Func<Task<byte[]>> retrieveAsync)
		{
			AddFluffySpoonLetsEncryptPersistence(services,
				new CustomCertificatePersistenceStrategy(
					persistAsync,
					retrieveAsync));
		}

		public static void AddFluffySpoonLetsEncryptPersistence(
		  this IServiceCollection services,
		  ICertificatePersistenceStrategy certificatePersistenceStrategy)
		{
			AddFluffySpoonLetsEncryptPersistence(services,
				(p) => certificatePersistenceStrategy);
		}

		public static void AddFluffySpoonLetsEncryptPersistence(
		  this IServiceCollection services,
		  Func<IServiceProvider, ICertificatePersistenceStrategy> certificatePersistenceStrategyFactory)
		{
			services.AddSingleton(certificatePersistenceStrategyFactory);
		}

		public static void AddFluffySpoonLetsEncryptFilePersistence(
		  this IServiceCollection services,
		  string relativeFilePath = "FluffySpoonAspNetLetsEncryptCertificate")
		{
			AddFluffySpoonLetsEncryptPersistence(services,
				new FileCertificatePersistenceStrategy(relativeFilePath));
		}

		public static void AddFluffySpoonLetsEncrypt(
		  this IServiceCollection services,
		  LetsEncryptOptions options)
		{
			services.AddSingleton<LetsEncryptCertificateContainer>();

			services.AddSingleton(options);

			services.AddHostedService<LetsEncryptRenewalHostedService>();
		}

		public static void UseFluffySpoonLetsEncrypt(
			this IApplicationBuilder app)
		{
			app.UseMiddleware<LetsEncryptMiddleware>();
		}
	}
}
