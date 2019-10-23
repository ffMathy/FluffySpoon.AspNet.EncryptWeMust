using System;
using System.Collections.Generic;
using System.Text;

namespace FluffySpoon.AspNet.LetsEncrypt
{
	[Flags]
	public enum ChallengeType
	{
		Http01 = 1,
		Dns01 = 2
	}
}
