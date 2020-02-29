namespace FluffySpoon.AspNet.LetsEncrypt.Certes
{
	/// <summary>
	/// Defines the method used to start the <see cref="ILetsEncryptRenewalService" /> to check for cert renewal.
	/// </summary>
	public enum StartUpMode
	{
		/// <summary>
		/// Startup and the initial LetsEncrypt cert renewal is initiated immediately when the <see cref="ILetsEncryptRenewalService" /> is started.
		/// </summary>
		Immediate,

		/// <summary>
		/// Startup and the initial LetsEncrypt cert renewal is initiated once the <see cref="ILetsEncryptRenewalService" /> challange handler is installed.
		/// </summary>
		Delayed,

		/// <summary>
		/// Startup and the initial LetsEncrypt cert renewal is initiated when the RunFluffySpoonLetsEncrypt method is called.
		/// </summary>
		Manual,
	}
}
