using System;
using Zak.Setup.Steps;

namespace SampleSetupDll
{
	[Serializable]
	public class StepSampleStep : SingleWorkflowStep
	{
		public override void Verify() { }
		public string Value { get; set; }
		public override string GetNodeType()
		{
			return "stepsample";
		}

		public override SingleWorkflowStep Undo()
		{
			return new UndoStepSampleStep {Value = Value};
		}

		public override bool Execute(ref string template)
		{
			var toShow = _setupFile.GetKey(Value);
			Console.WriteLine("Executing StepSampleStep with value '{0}'", toShow);
			return true;
		}
	}
}
