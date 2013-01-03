using System;

namespace Zak.Setup.Steps
{
	[Serializable]
	public class SetupAvailable : SingleWorkflowStep
	{
		public override string GetNodeType()
		{
			return null;
		}
		public override void Verify() { }

		public SetupAvailable()
		{
			//Proposals = new List<AskTellStep>();
			Value = null;
			IsNull = false;
		}

		public string Value { get; set; }
		public bool IsNull { get; set; }
		//public List<AskTellStep> Proposals { get; set; }
	}
}