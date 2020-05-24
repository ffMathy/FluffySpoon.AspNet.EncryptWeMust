using System;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;

namespace FluffySpoon.AspNet.LetsEncrypt.Azure
{
	public class AzureOptions
	{
		public string ResourceGroupName { get; set; }

		public AzureCredentials Credentials { get; set; }
		
		[Obsolete("This is no longer necessary, all matching apps & slots are automatically included")]
		public string Slot { get;set; }
	}
}
