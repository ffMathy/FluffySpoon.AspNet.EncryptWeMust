using System.ComponentModel.DataAnnotations;

namespace FluffySpoon.AspNet.LetsEncrypt.EntityFramework.Sample
{
	public class Challenge
	{
		[Key]
		public string Token { get; set; }
		public string Response { get; set; }
		public string Domains { get; set; }
	}
}
