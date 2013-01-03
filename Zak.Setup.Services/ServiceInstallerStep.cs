using System;
using System.Collections.Generic;
using Zak.Setup.Steps;

namespace Zak.Setup.Services
{
	[Serializable]
	public class ServiceInstallerStep : SingleWorkflowStep
	{
		public override bool NeedAdminRights { get { return true; } }
		private string _displayName;
		private string _fileName;
		private string _user;
		private string _role;
		private string _password;
		private ServiceBootFlag _startType;
		public string Name { get; set; }

		public override string GetNodeType()
		{
			return "serviceInstaller";
		}

		public override SingleWorkflowStep Undo()
		{
			return new ServiceUninstallerStep {Name = Name};
		}

		public override void Verify()
		{
			var pars = ParamStep.GetParameters(this);
			var si = new ServiceInstaller();
			Name = _setupFile.GetKey(Name);
	
			_displayName = GetIfExists(pars, "servicename");
			_fileName = GetIfExists(pars, "executable");
			_user = GetIfExists(pars, "user");
			_role = GetIfExists(pars, "role");
			if (_role == "LocalService")
			{
				_user = "NT AUTHORITY\\LocalService";
			}
			else if (_role == "NetworkService")
			{
				_user = "NT AUTHORITY\\NetworkService";
			}
			else if (_role == "LocalSystem")
			{
				_user = null;
			}
			_password = GetIfExists(pars, "password");

			_startType = ServiceBootFlag.Manual;
			if (!ServiceBootFlag.TryParse(GetIfExists(pars, "password"), out _startType))
			{
				_startType = ServiceBootFlag.Manual;
			}

			si.Verify(Name, _displayName, _fileName, _user, _password, _startType);
		}

		public override bool Execute(ref string template)
		{
			var si = new ServiceInstaller();
			
			try
			{
				si.Install(Name, _displayName, _fileName, _user, _password, _startType);
				return true;
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex);
				si.Uninstall(Name);
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
