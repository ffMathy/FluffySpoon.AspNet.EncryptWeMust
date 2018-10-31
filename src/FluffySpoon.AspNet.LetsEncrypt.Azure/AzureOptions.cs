using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;

namespace FluffySpoon.LetsEncrypt.Azure
{
	public class AzureOptions
	{
		public string ResourceGroupName { get; set; }

		public AzureCredentials Credentials { get; set; }
	}
}
