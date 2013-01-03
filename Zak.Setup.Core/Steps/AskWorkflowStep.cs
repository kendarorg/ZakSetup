using System;
using Zak.Setup.Commons;
using Zak.Setup.Enums;
using Zak.Setup.Steps;

namespace Zak.Setup.Core.Steps
{
	[Serializable]
	public class AskWorkflowStep : SingleWorkflowStep
	{
		public override string GetNodeType()
		{
			return "ask";
		}
		private const string DEFAULT_VALUE_APPLIED = "default_value_applied";
		private const int MAX_RETRY_COUNT = 3;

		public AskWorkflowStep()
		{
			For = null;
			ApplyOn = null;
			Value = null;
		}

		public string For { get; set; }
		public string ApplyOn { get; set; }
		public string Value { get; set; }

		public override bool Execute(ref string template)
		{
			DoStepAsk(this, ref template);
			return true;
		}

		private static void DoStepAsk(AskWorkflowStep workflowStep, ref string template)
		{
			//Retrieve the setup config for wich the question is made
			if (!string.IsNullOrEmpty(workflowStep.For) && _setupFile.SetupConfigs.ContainsKey(workflowStep.For))
			{
				HandleMultipleChoices(workflowStep, ref template);
			}
			else
			{
				HandleSpecificValueChoice(workflowStep, ref template);
			}
		}


		private static string GetEnv(string key)
		{
			string orkey = key;
			if (orkey.StartsWith("${"))
			{
				orkey = orkey.Substring(2).TrimEnd('}');
			}

			return CommandParser.GetEnv(orkey);
		}

		private static void HandleSpecificValueChoice(AskWorkflowStep workflowStep, ref string template)
		{
			Console.WriteLine(" Insert the value:");
			if (!string.IsNullOrEmpty(workflowStep.Value))
			{
				var possibleDefault = _setupFile.GetKey(workflowStep.Value);
				if (!string.IsNullOrEmpty(possibleDefault) && possibleDefault != workflowStep.Value)
				{
					workflowStep.Value = possibleDefault;
				}
				workflowStep.Value = DoAllReplaces(workflowStep.Value);
				Console.WriteLine(" Default: {0}", workflowStep.Value);
			}
			string lineRead = GetEnv(workflowStep.ApplyOn);

			if (DEFAULT_VALUE_APPLIED == lineRead)
			{
				lineRead = workflowStep.Value;
			}
			int maxTimes = MAX_RETRY_COUNT;
			while (string.IsNullOrEmpty(lineRead) && maxTimes>=0)
			{
				lineRead = Console.ReadLine();
				if (string.IsNullOrEmpty(lineRead))
				{
					if (!string.IsNullOrEmpty(workflowStep.Value))
					{
						Console.WriteLine("Applying Default");
						lineRead = workflowStep.Value;
					}
					else
					{
						maxTimes--;
						Console.WriteLine("Invalid value, retry!");
					}
				}
			}
			if (maxTimes < 0)
			{
				throw new Exception("Maximum retry count reached.");
			}
			if (lineRead == null)
			{
				throw new Exception("No value selected.");
			}
			var selectedValue = lineRead.Trim();
			template = template.Replace(workflowStep.ApplyOn, selectedValue);
			_setupFile.SetKey(workflowStep.ApplyOn, selectedValue);
			string orkey = workflowStep.ApplyOn;
			if (orkey.StartsWith("${"))
			{
				orkey = orkey.Substring(2).TrimEnd('}');
				_setupFile.SetKey(orkey + IfWorkflowStep.TEMPLATE_VALUE, selectedValue);
			}
		}

		private static void HandleMultipleChoices(AskWorkflowStep workflowStep, ref string template)
		{
			var configFor = _setupFile.SetupConfigs[workflowStep.For];

			int i;
			for (i = 0; i < configFor.SetupChoices.Count; i++)
			{
				var setupChoice = configFor.SetupChoices[i];
				Console.Write("-{0}: {1}", i, setupChoice.Value);
				if (!string.IsNullOrEmpty(setupChoice.Help))
					Console.WriteLine(" ({0})", setupChoice.Help);
				else
					Console.WriteLine("");
			}

			int selectedResult = -1;
			if (configFor.SetupChoices.Count > 1)
			{
				selectedResult = HandleMultipleAskChoices(workflowStep, selectedResult, i);
			}
			else
			{
				if (!int.TryParse(configFor.Default, out selectedResult))
				{
					selectedResult = 0;
				}
				Console.WriteLine("Selecting default!");
			}
			var selectedConfig = configFor.SetupChoices[selectedResult];

			string orkey = workflowStep.ApplyOn;
			if (orkey.StartsWith("${"))
			{
				orkey = orkey.Substring(2).TrimEnd('}');
			}

			if (selectedConfig.IsNull)
			{
				template = template.Replace("${" + workflowStep.ApplyOn + "}", string.Empty);
				_setupFile.SetKey(workflowStep.ApplyOn, string.Empty);
				_setupFile.SetKey(orkey + IfWorkflowStep.TEMPLATE_VALUE, string.Empty);
			}
			else if (configFor.ConfigType == ConfigTypes.Value)
			{
				template = template.Replace("${" + workflowStep.ApplyOn + "}", selectedConfig.Value);
				_setupFile.SetKey(workflowStep.ApplyOn, selectedConfig.Value);
				_setupFile.SetKey(orkey + IfWorkflowStep.TEMPLATE_VALUE, selectedConfig.Value);
			}
			else
			{
				var newTemplate = _setupFile.SetupTemplates[selectedConfig.Value].Content;
				foreach (var step in selectedConfig.WorkflowSteps)
				{
					_setupFile.AddUndo(step.Undo());
					step.Execute(ref newTemplate);
				}
				
				newTemplate = DoAllReplaces(newTemplate);
				template = template.Replace("${" + workflowStep.ApplyOn + "}", newTemplate);
				_setupFile.SetKey(workflowStep.ApplyOn, newTemplate);
				_setupFile.SetKey(orkey + IfWorkflowStep.TEMPLATE_VALUE, selectedConfig.Value);
			}
		}

		private static int HandleMultipleAskChoices(AskWorkflowStep workflowStep, int selectedResult, int i)
		{
			var applyOn = GetEnv(workflowStep.ApplyOn);
			if (applyOn != null)
			{
				if (!int.TryParse(applyOn, out selectedResult))
				{
					selectedResult = -1;
				}
			}
			int maxTimes = MAX_RETRY_COUNT;
			while (selectedResult == -1 && maxTimes >= 0)
			{
				Console.WriteLine("Select 0-{0}", (i - 1));
				var result = Console.ReadLine();
				if (!int.TryParse(result, out selectedResult))
				{
					maxTimes--;
					Console.WriteLine("Invalid value, retry!");
					selectedResult = -1;
				}
			}
			if (maxTimes < 0 && selectedResult==-1)
			{
				throw new Exception("Maximum retry count reached.");
			}
			return selectedResult;
		}

	}
}