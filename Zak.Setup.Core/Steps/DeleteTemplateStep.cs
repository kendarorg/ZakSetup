using System;
using System.IO;
using Zak.Setup.Steps;

namespace Zak.Setup.Core.Steps
{
	[Serializable]
	public class DeleteTemplateStep:SingleWorkflowStep
	{
		public override string GetNodeType()
		{
			return "deleteTemplate";
		}
		public DeleteTemplateStep()
		{
			From = null;
		}


		public override void Verify()
		{

		}

		public override bool Execute(ref string template)
		{
			var from = DoAllReplaces(From);
			if(File.Exists(from)) File.Delete(from);
			return true;
		}
		
		public string From { get; set; }
	}
}
