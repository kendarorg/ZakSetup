using System;
using Zak.Setup.Steps;

namespace Zak.Setup.Services
{
	[Serializable]
	public class ServiceUninstallerStep : SingleWorkflowStep
	{
		public string Name { get; set; }

		public override string GetNodeType()
		{
			return "serviceuninstaller";
		}

		public override bool Execute(ref string template)
		{
			var si = new ServiceInstaller();
			string serviceName = Name;
			try
			{
				si.Uninstall(serviceName);
				return true;
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex);
				return false;
			}
		}
	}
}