using System.Xml;
using Zak.Setup.Steps;

namespace Zak.Setup.Factories
{
	public abstract class BaseStepFactory
	{
		public abstract SingleWorkflowStep Create(string workflowType, XmlNode node);
	}
}
