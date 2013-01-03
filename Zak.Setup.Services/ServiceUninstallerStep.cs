using System;
using Zak.Setup.Steps;

namespace Zak.Setup.Services
{
	[Serializable]
	public class ServiceUninstallerStep : SingleWorkflowStep
	{
		public override bool NeedAdminRights { get { return true; } }
		public string Name { get; set; }

		public override string GetNodeType()
		{
			return "serviceuninstaller";
		}

		public override void Verify()
		{
			
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
				if (!_setupFile.Undoing)
				{
					Console.WriteLine(ex);	
				}
				
				return false;
			}
		}
	}
}