using System;

namespace Zak.Setup.Setup
{
	[Serializable]
	public class BaseTemplateClass
	{
		public BaseTemplateClass()
		{
			Content = string.Empty;
			Id = Guid.NewGuid().ToString();
		}

		public string Id { get; set; }
		public string Content { get; set; }
	}
}