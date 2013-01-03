using System;
using System.Collections.Generic;
using Zak.Setup.Enums;
using Zak.Setup.Steps;

namespace Zak.Setup.Setup
{
	[Serializable]
	public class SetupConfig : SetupWithHelp
	{
		public SetupConfig()
		{
			AllowNone = false;
			ConfigType = ConfigTypes.Value;
			Key = null;
			SetupChoices = new List<SetupAvailable>();
			Default = null;
		}

		public bool AllowNone { get; set; }
		public ConfigTypes ConfigType { get; set; }
		public string Key { get; set; }
		public List<SetupAvailable> SetupChoices { get; set; }
		public string Default { get; set; }
	}
}