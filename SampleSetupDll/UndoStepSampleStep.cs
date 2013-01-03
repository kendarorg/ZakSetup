using System;
using Zak.Setup.Steps;

namespace SampleSetupDll
{
	[Serializable]
	public class UndoStepSampleStep : SingleWorkflowStep
	{
		public string Value { get; set; }
		public override string GetNodeType()
		{
			return "undostepsample";
		}

		public override SingleWorkflowStep Undo()
		{
			return new StepSampleStep { Value = Value };
		}

		public override bool Execute(ref string template)
		{
			var toShow = _setupFile.GetKey(Value);
			Console.WriteLine("Executing UndoStepSampleStep with value '{0}'", toShow);
			return true;
		}
	}
}
