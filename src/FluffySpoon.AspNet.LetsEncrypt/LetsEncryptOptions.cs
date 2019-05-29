using System;
using System.Collections.Generic;
using Certes;

namespace FluffySpoon.AspNet.LetsEncrypt
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
		/// Required. Sent to LetsEncrypt to let them know what details you want in your certificate. Some of the properties are optional.
		/// </summary>
		public CsrInfo CertificateSigningRequest { get; set; }

		/// <summary>
		/// Gets or sets the renewal fail mode - i.e. what happens if an exception is thrown in the certificate renewal process.
		/// </summary>
		public RenewalFailMode RenewalFailMode { get; set; } = RenewalFailMode.LogAndContinue;
	}
}
