using System;
using System.Collections.Generic;
using Zak.Setup.Steps;

namespace Zak.Setup.Services
{
	[Serializable]
	public class ServiceInstallerStep : SingleWorkflowStep
	{
		public string Name { get; set; }

		public override string GetNodeType()
		{
			return "serviceInstaller";
		}

		public override SingleWorkflowStep Undo()
		{
			return new ServiceUninstallerStep {Name = Name};
		}

		public override bool Execute(ref string template)
		{
			var pars = ParamStep.GetParameters(this);
			var si = new ServiceInstaller();
			string serviceName = _setupFile.GetKey(Name);
			try
			{
				string displayName = GetIfExists(pars, "servicename");
				string fileName = GetIfExists(pars, "executable");
				string user = GetIfExists(pars, "user");
				string role = GetIfExists(pars, "role");
				if (role == "LocalService")
				{
					user = "NT AUTHORITY\\LocalService";
				}
				else if (role == "NetworkService")
				{
					user = "NT AUTHORITY\\NetworkService";
				}
				else if (role == "LocalSystem")
				{
					user = null;
				}
				string password = GetIfExists(pars, "password");
				
				ServiceBootFlag startType;
				if (!ServiceBootFlag.TryParse(GetIfExists(pars, "password"), out startType))
				{
					startType = ServiceBootFlag.Manual;
				}

				si.Install(serviceName, displayName, fileName, user, password, startType);
				return true;
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex);
				si.Uninstall(serviceName);
				return false;
			}
		}

		private string GetIfExists(Dictionary<string, string> pars, string name,string def=null)
		{
			name = name.ToLowerInvariant();
			if (!pars.ContainsKey(name)) return def;
			if (pars[name].StartsWith("${") && pars[name].EndsWith("}")) return def;
			return pars[name];
		}
	}
}
