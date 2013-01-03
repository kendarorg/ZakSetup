using System;
using System.Collections.Generic;
using Zak.Setup.Steps;

namespace Zak.Setup.Core.Steps
{
	[Serializable]
	public class IfWorkflowStep : SingleWorkflowStep
	{
		public override string GetNodeType()
		{
			return "if";
		}
		public override void Verify() { }
		public string Value { get; set; }
		public string Is { get; set; }
		public bool MustBeTrue { get; set; }

		public IfWorkflowStep()
		{
			Value = null;
			Is = null;
			MustBeTrue = true;
		}

		internal bool IsTrue
		{
			get
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
					/*base.RunSteps(workflowSteps, ref template);
					return;*/
					val = Value;
				}

				bool doSomething = (val == null && Is == null);
				if (!doSomething && val != null && Is != null)
				{
					doSomething = val == Is;
				}
				if ((doSomething && MustBeTrue) || (!doSomething && !MustBeTrue)) return true;
					return false;
			}
		}

		internal void RunStepsBase(List<SingleWorkflowStep> workflowSteps, ref string template)
		{
			base.RunSteps(workflowSteps, ref template);
		}

		public override void RunSteps(List<SingleWorkflowStep> workflowSteps, ref string template)
		{
			if (IsTrue)
			{
				base.RunSteps(workflowSteps, ref template);
			}
		}
	}

}
