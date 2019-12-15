namespace FluffySpoon.AspNet.LetsEncrypt.Persistence
{
	public class ChallengeDto
	{
		public string Token { get; set; }
		public string Response { get; set; }
		public string[] Domains { get; set; }
	}
}
