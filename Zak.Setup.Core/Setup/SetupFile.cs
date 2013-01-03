using System;
using System.Collections.Generic;
using Zak.Setup.Core.Steps;
using Zak.Setup.Setup;
using Zak.Setup.Steps;

namespace Zak.Setup.Core.Setup
{
	[Serializable]
	public class SetupFile : ISetupFile
	{
		public bool Unattended { get; set; }

		public SetupFile(bool unattended)
		{
			Undoing = false;
			Unattended = unattended;
			SetupConfigs = new Dictionary<string, SetupConfig>();
			SetupTemplates = new Dictionary<string, SetupTemplate>();
			BaseTemplates = new Dictionary<string, SetupBaseTemplate>();
			CollectedArrays = new Dictionary<string, List<string>>();
			WorkflowRoot = new WorkflowRootStep();
			CollectedValues = new Dictionary<string, string>();
			Rollback = new List<SingleWorkflowStep>();
		}

		public Dictionary<string, string> CollectedValues { get; set; }
		public Dictionary<string, List<string>> CollectedArrays { get; set; }
		public Dictionary<string, SetupConfig> SetupConfigs { get; set; }
		public Dictionary<string, SetupTemplate> SetupTemplates { get; set; }
		public Dictionary<string, SetupBaseTemplate> BaseTemplates { get; set; }
		public SingleWorkflowStep WorkflowRoot { get; set; }
		public List<SingleWorkflowStep> Rollback { get; set; }

		public bool Undoing { get; set; }

		public string PluginsDir { get; set; }

		public void SetKey(string key, string val)
		{
			string orkey = key;
			if (orkey.StartsWith("${"))
			{
				orkey = orkey.Substring(2).TrimEnd('}');
			}
			if (orkey.EndsWith("[]"))
			{
				if (!CollectedArrays.ContainsKey(orkey))
				{
					CollectedArrays.Add(orkey,new List<string>());
				}
				CollectedArrays[orkey].Add(val);
			}
			else
			{
				if (CollectedValues.ContainsKey(orkey)) CollectedValues[orkey] = val;
				else CollectedValues.Add(orkey, val);
			}
		}

		public string GetKey(string key)
		{
			string orkey = key;
			if (orkey.StartsWith("${"))
			{
				orkey = orkey.Substring(2).TrimEnd('}');
			}
			string toret = null;
			if (CollectedValues.ContainsKey(orkey)) toret = CollectedValues[orkey];
			if (CollectedArrays.ContainsKey(orkey)) toret = string.Join("|",CollectedArrays[orkey]);
			
			return toret == null ? key : toret;
		}

		public void AddUndo(SingleWorkflowStep undo)
		{
			if (Undoing) return;
			if (undo != null) Rollback.Add(undo);
		}
	}
}
