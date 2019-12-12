using FluffySpoon.AspNet.LetsEncrypt.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Linq;
using System.Threading.Tasks;
using FluffySpoon.AspNet.LetsEncrypt.Logic;

namespace FluffySpoon.AspNet.LetsEncrypt
{
	public static class RegistrationExtensions
	{
		private static void AddFluffySpoonLetsEncryptPersistenceService(
			this IServiceCollection services)
		{
			if(services.Any(x => x.ServiceType == typeof(IPersistenceService)))
				return;
			
			services.AddSingleton<IPersistenceService, PersistenceService>();
		}

		public static void AddFluffySpoonLetsEncryptRenewalLifecycleHook<TCertificateRenewalLifecycleHook>(
			this IServiceCollection services) where TCertificateRenewalLifecycleHook : class, ICertificateRenewalLifecycleHook
		{
			services.AddFluffySpoonLetsEncryptPersistenceService();
			services.AddSingleton<ICertificateRenewalLifecycleHook, TCertificateRenewalLifecycleHook>();
		}

		public static void AddFluffySpoonLetsEncryptCertificatePersistence(
			this IServiceCollection services,
			Func<CertificateType, byte[], Task> persistAsync,
			Func<CertificateType, Task<byte[]>> retrieveAsync)
		{
			AddFluffySpoonLetsEncryptCertificatePersistence(services,
				new CustomCertificatePersistenceStrategy(
					persistAsync,
					retrieveAsync));
		}

		public static void AddFluffySpoonLetsEncryptCertificatePersistence(
		  this IServiceCollection services,
		  ICertificatePersistenceStrategy certificatePersistenceStrategy)
		{
			AddFluffySpoonLetsEncryptCertificatePersistence(services,
				(p) => certificatePersistenceStrategy);
		}

		public static void AddFluffySpoonLetsEncryptCertificatePersistence(
		  this IServiceCollection services,
		  Func<IServiceProvider, ICertificatePersistenceStrategy> certificatePersistenceStrategyFactory)
		{
			services.AddFluffySpoonLetsEncryptPersistenceService();
			services.AddSingleton(certificatePersistenceStrategyFactory);
		}

		public static void AddFluffySpoonLetsEncryptFileCertificatePersistence(
		  this IServiceCollection services,
		  string relativeFilePath = "FluffySpoonAspNetLetsEncryptCertificate")
		{
			AddFluffySpoonLetsEncryptCertificatePersistence(services,
				new FileCertificatePersistenceStrategy(relativeFilePath));
		}

		public static void AddFluffySpoonLetsEncryptChallengePersistence(
			this IServiceCollection services,
			PersistChallengesDelegate persistAsync,
			RetrieveChallengesDelegate retrieveAsync,
			DeleteChallengesDelegate deleteAsync)
		{
			AddFluffySpoonLetsEncryptChallengePersistence(services,
				new CustomChallengePersistenceStrategy(
					persistAsync,
					retrieveAsync,
					deleteAsync));
		}

		public static void AddFluffySpoonLetsEncryptChallengePersistence(
		  this IServiceCollection services,
		  IChallengePersistenceStrategy certificatePersistenceStrategy)
		{
			AddFluffySpoonLetsEncryptChallengePersistence(services,
				(p) => certificatePersistenceStrategy);
		}

		public static void AddFluffySpoonLetsEncryptChallengePersistence(
		  this IServiceCollection services,
		  Func<IServiceProvider, IChallengePersistenceStrategy> certificatePersistenceStrategyFactory)
		{
			services.AddFluffySpoonLetsEncryptPersistenceService();
			services.AddSingleton(certificatePersistenceStrategyFactory);
		}

		public static void AddFluffySpoonLetsEncryptFileChallengePersistence(
		  this IServiceCollection services,
		  string relativeFilePath = "FluffySpoonAspNetLetsEncryptChallenge")
		{
			AddFluffySpoonLetsEncryptChallengePersistence(services,
				new FileChallengePersistenceStrategy(relativeFilePath));
		}

		public static void AddFluffySpoonLetsEncryptMemoryChallengePersistence(
		  this IServiceCollection services)
		{	
			AddFluffySpoonLetsEncryptChallengePersistence(
				services,
				new MemoryChallengePersistenceStrategy());
		}

		public static void AddFluffySpoonLetsEncryptMemoryCertficatesPersistence(
		  this IServiceCollection services)
		{
			AddFluffySpoonLetsEncryptCertificatePersistence(
				services,
				new MemoryCertificatePersistenceStrategy());
		}

		public static void AddFluffySpoonLetsEncryptRenewalService(
		  this IServiceCollection services,
		  LetsEncryptOptions options)
		{
			services.AddFluffySpoonLetsEncryptPersistenceService();
			services.AddSingleton(options);
			services.AddSingleton<ILetsEncryptClient, LetsEncryptClient>();
			services.AddSingleton<ICertificateProvider, CertificateProvider>();
			services.AddTransient<ILetsEncryptRenewalService, LetsEncryptRenewalService>();
			services.AddTransient<IHostedService, LetsEncryptRenewalService>();
		}

		public static void UseFluffySpoonLetsEncryptChallengeApprovalMiddleware(
			this IApplicationBuilder app)
		{
			app.UseMiddleware<LetsEncryptChallengeApprovalMiddleware>();
		}
	}
}
