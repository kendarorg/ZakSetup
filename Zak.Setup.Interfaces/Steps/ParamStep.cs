using System;
using System.Collections.Generic;

namespace Zak.Setup.Steps
{
	[Serializable]
	public class ParamStep : SingleWorkflowStep
	{
		public string Value { get; set; }
		public string Name { get; set; }

		public override string GetNodeType()
		{
			return "param";
		}

		public static Dictionary<string,string> GetParameters(SingleWorkflowStep step)
		{
			var pars = new Dictionary<string, string>();
			foreach (var item in step.WorkflowSteps)
			{
				var param = item as ParamStep;
				if (param!=null)
				{
					var realValue = _setupFile.GetKey(param.Value);
					pars.Add(param.Name.ToLowerInvariant(), realValue);
				}
			}
			return pars;
		}
	}
}
