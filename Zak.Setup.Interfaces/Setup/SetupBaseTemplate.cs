namespace Zak.Setup.Setup
{
	public class SetupBaseTemplate : BaseTemplateClass
	{
		public SetupBaseTemplate()
		{
			TemplateSourceFile = null;
			IsXml = false;
		}

		public string TemplateSourceFile { get; set; }
		public bool IsXml { get; set; }
	}
}