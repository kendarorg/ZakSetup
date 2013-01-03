using System;
using Zak.Setup.Steps;

namespace Zak.Setup.Core.Steps
{
	[Serializable]
	public class SwitchWorkflowStep : SingleWorkflowStep
	{
		public string Value { get; set; }
		public override void Verify() { }

		public override string GetNodeType()
		{
			return "switch";
		}
		public override bool Execute(ref string template)
		{
			string orkey = Value;
			string val;
			if (orkey.StartsWith("${"))
			{
				orkey = orkey.Substring(2).TrimEnd('}');
				val = _setupFile.GetKey(orkey + TEMPLATE_VALUE);
				if (val == orkey + TEMPLATE_VALUE) val = null;
			}
			else
			{
				val = Value;
			}
			var cloneNode = (SwitchWorkflowStep)Clone();
			foreach (var item in cloneNode.WorkflowSteps)
			{
				var caseStep = item as CaseWorkflowStep;
				if (caseStep != null)
				{
					if (caseStep.Value == val)
					{
						caseStep.RunSteps(caseStep.WorkflowSteps,ref template);
						return true;
					}
				}
			}
			return false;
		}
	}
}
