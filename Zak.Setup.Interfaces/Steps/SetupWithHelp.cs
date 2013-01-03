using System;

namespace Zak.Setup.Steps
{
	[Serializable]
	public class SetupWithHelp
	{
		public SetupWithHelp()
		{
			Help = null;
		}

		public string Help { get; set; }
	}
}