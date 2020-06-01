namespace FluffySpoon.AspNet.EncryptWeMust.Persistence
{
	public class ChallengeDto
	{
		public string Token { get; set; }
		public string Response { get; set; }
		public string[] Domains { get; set; }

		public override string ToString()
		{
			return $"Token: {Token}";
		}
	}
}
