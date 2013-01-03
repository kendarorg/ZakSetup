using System.Collections.Generic;
using Zak.Setup.Steps;

namespace Zak.Setup.Setup
{
	public interface ISetupFile
	{
		void SetKey(string key, string val);
		string GetKey(string key);
		void AddUndo(SingleWorkflowStep undo);
		Dictionary<string, string> CollectedValues { get; set; }
		Dictionary<string, List<string>> CollectedArrays { get; set; }
		Dictionary<string, SetupConfig> SetupConfigs { get; set; }
		Dictionary<string, SetupTemplate> SetupTemplates { get; set; }
		Dictionary<string, SetupBaseTemplate> BaseTemplates { get; set; }
		SingleWorkflowStep WorkflowRoot { get; set; }
		List<SingleWorkflowStep> Rollback { get; set; }
		bool Unattended { get; set; }
	}
}