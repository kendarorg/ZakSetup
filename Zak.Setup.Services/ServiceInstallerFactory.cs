using System.Xml;
using Zak.Setup.Factories;
using Zak.Setup.Steps;

namespace Zak.Setup.Services
{
	public class ServiceInstallerFactory:BaseStepFactory
	{
		public override SingleWorkflowStep Create(string workflowType, XmlNode node)
		{
			SingleWorkflowStep toret = null;
			switch (workflowType)
			{
				case ("SERVICEINSTALLER"):
					{
						toret = new ServiceInstallerStep
							{
								Name = node.GetAttribute("name")
							};
					}
					break;
				case ("SERVICEUNINSTALLER"):
					{
						toret = new ServiceUninstallerStep
						{
							Name = node.GetAttribute("name")
						};
					}
					break;
			}
			return toret;
		}
	}
}
