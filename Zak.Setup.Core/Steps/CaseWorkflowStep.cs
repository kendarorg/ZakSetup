using System;
using Zak.Setup.Steps;

namespace Zak.Setup.Core.Steps
{
	[Serializable]
	public class CaseWorkflowStep : SingleWorkflowStep
	{
		public string Value { get; set; }
		public override void Verify() { }

		public override string GetNodeType()
		{
			return "case";
		}
	}
}
