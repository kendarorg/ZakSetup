using System;
using System.Xml;
using Zak.Setup;
using Zak.Setup.Factories;
using Zak.Setup.Steps;

namespace SampleSetupDll
{
	public class StepSampleInstallerFactory : BaseStepFactory
	{
		public override SingleWorkflowStep Create(string workflowType, XmlNode node)
		{
			SingleWorkflowStep toret = null;
			switch (workflowType)
			{
				case ("STEPSAMPLE"):
					{
						toret = new StepSampleStep
						{
							Value = node.GetAttribute("value")
						};
					}
					break;
				case ("UNDOSTEPSAMPLE"):
					{
						toret = new UndoStepSampleStep
						{
							Value = node.GetAttribute("value")
						};
					}
					break;
			}
			return toret;
		}
	}
}
