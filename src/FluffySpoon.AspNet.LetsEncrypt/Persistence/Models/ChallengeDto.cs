using System;
using System.Collections.Generic;
using System.Text;

namespace FluffySpoon.AspNet.LetsEncrypt.Persistence.Models
{
	public class ChallengeDto
	{
		public string Token { get; set; }
		public string Response { get; set; }
	}
}
