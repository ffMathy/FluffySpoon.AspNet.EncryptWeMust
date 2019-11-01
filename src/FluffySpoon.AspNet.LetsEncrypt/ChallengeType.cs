using System;
using System.Collections.Generic;
using System.Text;

namespace FluffySpoon.AspNet.LetsEncrypt
{
	public enum ChallengeType
	{
		Http01 = 1,
		Dns01 = 2
	}
}
