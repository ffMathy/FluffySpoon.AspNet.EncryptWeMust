
using Amazon.Runtime;

namespace FluffySpoon.AspNet.LetsEncrypt.Aws
{
	public class AwsOptions
	{
		public Amazon.RegionEndpoint Region { get; set; }
		public AWSCredentials Credentials { get; set; }
	}
}
