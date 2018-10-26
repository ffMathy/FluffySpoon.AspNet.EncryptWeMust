using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using Certes.Acme;

namespace FluffySpoon.AspNet.LetsEncrypt
{
	public class LetsEncryptCertificateContainer
	{
		internal IEnumerable<IChallengeContext> PendingChallengeContexts { get; set; }
		public X509Certificate2 Certificate { get; internal set; }

		private static LetsEncryptCertificateContainer instance;
		public static LetsEncryptCertificateContainer Instance
		{
			get
			{
				lock (typeof(LetsEncryptCertificateContainer))
				{
					if (instance == null)
						throw new InvalidOperationException("The certificate container has not yet been initialized.");

					return instance;
				}
			}
		}

		public LetsEncryptCertificateContainer()
		{
			lock (typeof(LetsEncryptCertificateContainer))
			{
				if(instance != null)
					throw new InvalidOperationException("The certificate container has already been initialized.");

				instance = this;
			}
		}
	}
}
