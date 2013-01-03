
using System;

namespace Zak.Setup.Core.Steps
{
	[Serializable]
	public class DoWhileStep:IfWorkflowStep
	{
		public override string GetNodeType()
		{
			return "dowhile";
		}

		public override bool Execute(ref string template)
		{
			DoWhileStep cloneNode;
			do
			{
				cloneNode = (DoWhileStep)Clone();
				RunStepsBase(cloneNode.WorkflowSteps, ref template);
			} while (cloneNode.IsTrue);
			return true;
		}
	}
}
