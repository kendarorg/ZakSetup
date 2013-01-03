using System;
using System.Text.RegularExpressions;
using Zak.Setup.Steps;

namespace Zak.Setup.Core.Steps
{
	[Serializable]
	public class TellWorkflowStep : SingleWorkflowStep
	{
		public override string GetNodeType()
		{
			return "tell";
		}
		public override void Verify() { }

		public TellWorkflowStep()
		{
			ApplyOn = null;
			Value = null;
		}
		public string ApplyOn { get; set; }
		public string Value { get; set; }

		public override bool Execute(ref string template)
		{
			var val = Value;

			if (!string.IsNullOrEmpty(val))
			{
				var r = new Regex(@"(\${[^\}]*})");
				var n = r.Split(val);

				foreach (var m in n)
				{
					if (m.StartsWith("${") && m.EndsWith("}"))
					{
						var newVal = _setupFile.GetKey(m);
						val = val.Replace(m, newVal);
					}
				}
			}

			if (!ApplyOn.EndsWith("[]}") && !ApplyOn.EndsWith("[]"))
			{
				template = template.Replace(ApplyOn, val);
			}
			
			_setupFile.SetKey(ApplyOn, val);
			string orkey = ApplyOn;
			if (orkey.StartsWith("${"))
			{
				orkey = orkey.Substring(2).TrimEnd('}');
			}
			_setupFile.SetKey(orkey + IfWorkflowStep.TEMPLATE_VALUE, val);
			
			return true;
		}
	}
}