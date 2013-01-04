using System;
using System.Collections.Generic;
using Zak.Setup.Setup;

namespace Zak.Setup.Steps
{
	[Serializable]
	public abstract class SingleWorkflowStep : SetupWithHelp,ICloneable
	{
		public const string TEMPLATE_VALUE = "_TEMPLATE_VALUE_";
		protected static ISetupFile _setupFile;

		public virtual bool NeedAdminRights { get { return false; } }
		public virtual bool NeedHighestAvailableRights { get { return false; } }
		public virtual bool NeedUiAccess { get { return false; } }

		protected SingleWorkflowStep()
		{
			WorkflowSteps = new List<SingleWorkflowStep>();
		}

		protected static string DoAllReplaces(string src)
		{
			foreach (var kvp in _setupFile.CollectedValues)
			{
				src = src.Replace("${" + kvp.Key + "}", kvp.Value);
			}
			return src;
		}

		public static void Initialize(ISetupFile setupFile)
		{
			_setupFile = setupFile;
		}

		public List<SingleWorkflowStep> WorkflowSteps { get; set; }

		public virtual SingleWorkflowStep Undo()
		{
			return null;
		}

		public virtual bool Execute(ref string template)
		{
			return true;
		}

		public virtual void RunSteps(List<SingleWorkflowStep> workflowSteps, ref string template)
		{
			foreach (var workflowStep in workflowSteps)
			{
				ShowElementHelp(workflowStep);
				workflowStep.Verify();
				_setupFile.AddUndo(workflowStep.Undo());
				workflowStep.Execute(ref template);
				workflowStep.RunSteps(workflowStep.WorkflowSteps, ref template);
			}
		}

		public abstract void Verify();


		public static void ShowElementHelp(SetupWithHelp workflowStep, string alternateContent = null)
		{
			var help = string.Empty; 
			if(workflowStep.Help!=null) help=DoAllReplaces(workflowStep.Help);
			if (alternateContent != null) alternateContent = DoAllReplaces(alternateContent);
			if (!string.IsNullOrEmpty(help))
			{
				Console.WriteLine("Hint: {0}", help);
			}
			if (!string.IsNullOrEmpty(alternateContent))
			{
				Console.WriteLine(alternateContent);
			}
		}

		public abstract string GetNodeType();

		public virtual object Clone()
		{
			var step = (SingleWorkflowStep)Activator.CreateInstance(GetType());
			foreach (var property in GetType().GetProperties())
			{
				if (property.CanRead && property.CanWrite && property.Name != "WorkflowSteps")
				{
					var thisValue = property.GetValue(this, null);
					property.SetValue(step,thisValue,null);
				}
			}
			for (int i = 0; i < WorkflowSteps.Count;i++ )
			{
				step.WorkflowSteps.Add((SingleWorkflowStep)WorkflowSteps[i].Clone());
			}
			return step;
		}
	}
}