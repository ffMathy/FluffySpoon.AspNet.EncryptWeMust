using System;
using System.Collections.Generic;
using Certes;
using Certes.Acme;

namespace FluffySpoon.AspNet.LetsEncrypt.Certes
{
	public class LetsEncryptOptions
	{
		public IEnumerable<string> Domains { get; set; }

		/// <summary>
		/// Used only for LetsEncrypt to contact you when the domain is about to expire - not actually validated.
		/// </summary>
		public string Email { get; set; }

		/// <summary>
		/// The amount of time before the expiry date of the certificate that a new one is created. Defaults to 30 days.
		/// </summary>
		public TimeSpan? TimeUntilExpiryBeforeRenewal { get; set; } = TimeSpan.FromDays(30);

		/// <summary>
		/// The amount of time after the last renewal date that a new one is created. Defaults to null.
		/// </summary>
		public TimeSpan? TimeAfterIssueDateBeforeRenewal { get; set; } = null;

		/// <summary>
		/// Recommended while testing - increases your rate limit towards LetsEncrypt. Defaults to false.
		/// </summary>
		public bool UseStaging { get; set; }
		
		/// <summary>
		/// Gets the uri which will be used to talk to LetsEncrypt servers.
		/// </summary>
		public Uri LetsEncryptUri => UseStaging
			? WellKnownServers.LetsEncryptStagingV2
			: WellKnownServers.LetsEncryptV2;

		/// <summary>
		/// Required. Sent to LetsEncrypt to let them know what details you want in your certificate. Some of the properties are optional.
		/// </summary>
		public CsrInfo CertificateSigningRequest { get; set; }

		/// <summary>
		/// Gets or sets the renewal fail mode - i.e. what happens if an exception is thrown in the certificate renewal process.
		/// </summary>
		public RenewalFailMode RenewalFailMode { get; set; } = RenewalFailMode.LogAndContinue;

		/// <summary>
		/// Gets or sets the <see cref="Certes.KeyAlgorithm"/> used to request a new LetsEncrypt certificate.
		/// </summary>
		public KeyAlgorithm KeyAlgorithm { get; set; } = KeyAlgorithm.ES256;

		/// <summary>
		/// Get or set a delay before the initial run of the renewal service (subsequent runs will be at 1hr intervals)
		/// On some platform/deployment systems (e.g Azure Slot Swap) we do not want the renewal service to start immediately, because we may not
		/// yet have incoming requests (e.g. for challenges) directed to us. 
		/// </summary>
		public TimeSpan RenewalServiceStartupDelay { get; set; } = TimeSpan.Zero;
	}
}
