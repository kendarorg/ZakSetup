using System;
using Zak.Setup.Steps;

namespace Zak.Setup.Core.Steps
{
	[Serializable]
	public class IncludeStep : SingleWorkflowStep
	{
		public override void Verify() { }
		public override string GetNodeType()
		{
			return "include";
		}
	}
}
