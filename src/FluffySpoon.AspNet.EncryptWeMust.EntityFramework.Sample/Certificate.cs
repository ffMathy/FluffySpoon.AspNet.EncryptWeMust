using System.ComponentModel.DataAnnotations;

namespace FluffySpoon.AspNet.EncryptWeMust.EntityFramework.Sample
{
	public class Certificate
	{
		[Key]
		public string Key { get; set; }

		public byte[] Bytes { get; set; }

		public Certificate()
		{
		}
	}
}