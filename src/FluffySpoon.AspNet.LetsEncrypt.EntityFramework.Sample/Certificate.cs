using System;
using System.ComponentModel.DataAnnotations;

namespace FluffySpoon.AspNet.LetsEncrypt.EntityFramework.Sample
{
	public class Certificate
	{
		[Key]
		public Guid Id { get; set; }

		public byte[] Bytes { get; set; }

		public Certificate()
		{
		}
	}
}