using System;
using Zak.Setup.Steps;

namespace Zak.Setup.Core.Steps
{
	[Serializable]
	public class WorkflowRootStep : SingleWorkflowStep
	{
		public override string GetNodeType()
		{
			return "root";
		}

		public override void Verify() { }

		public override bool Execute(ref string template)
		{
			RunSteps(WorkflowSteps,ref template);
			return true;
		}
	}
}
