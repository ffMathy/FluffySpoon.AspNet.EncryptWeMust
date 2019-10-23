namespace FluffySpoon.AspNet.LetsEncrypt.Persistence.Models
{
	public class ChallengeDto
	{
		public string Token { get; set; }
		public string Response { get; set; }
		public ChallengeType Type { get; set; }
		public string[] Domains { get; set; }
	}
}
