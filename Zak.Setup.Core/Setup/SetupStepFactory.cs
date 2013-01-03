using System.Collections.Generic;
using System.Xml;
using Zak.Setup.Core.Steps;
using Zak.Setup.Factories;
using Zak.Setup.Steps;


namespace Zak.Setup.Core.Setup
{
	public static class SetupStepFactory
	{
		private static readonly Dictionary<string,BaseStepFactory> _factories = new Dictionary<string, BaseStepFactory>();

		public static void AddFactory(BaseStepFactory factory)
		{
			if (factory == null) return;
			var name = factory.GetType().FullName;
			if (!_factories.ContainsKey(name))
			{
				_factories.Add(name, factory);	
			}
			
		}
		public static SingleWorkflowStep CreateStep(XmlNode node)
		{
			SingleWorkflowStep toret = null;
			string workflowType = node.Name.ToUpperInvariant();
			switch (workflowType)
			{
				case ("IF"):
					{
						toret = new IfWorkflowStep
						{
							Help = node.GetAttribute("help"),
							Is = node.GetAttribute("is"),
							Value = node.GetAttribute("value"),
							MustBeTrue = node.IsAttributeSet("not") == false
						};
					}
					break;
				case ("DOWHILE"):
					{
						toret = new DoWhileStep
						{
							Help = node.GetAttribute("help"),
							Is = node.GetAttribute("is"),
							Value = node.GetAttribute("value"),
							MustBeTrue = node.IsAttributeSet("not") == false
						};
					}
					break;
				case ("DELETE"):
					{
						toret = new DeleteWorkflowStep
						{
							Help = node.GetAttribute("help"),
							From = node.GetAttribute("from")
						};
					}
					break;
				case ("DELETETEMPLATE"):
					{
						toret = new DeleteTemplateStep
						{
							From = node.GetAttribute("from")
						};
					}
					break;
				case ("ASK"):
					{
						toret = new AskWorkflowStep
						{
							Help = node.GetAttribute("help"),
							For = node.GetAttribute("for"),
							ApplyOn = node.GetAttribute("applyOn"),
							Value = node.GetAttribute("value")
						};
					}
					break;
				case ("COPY"):
					{
						toret = new CopyWorkflowStep
						{
							Help = node.GetAttribute("help"),
							From = node.GetAttribute("from"),
							To = node.GetAttribute("to"),
							What = node.GetAttribute("what")
						};

					}
					break;
				case ("TELL"):
					{
						toret = new TellWorkflowStep
						{
							Help = node.GetAttribute("help"),
							ApplyOn = node.GetAttribute("applyOn"),
							Value = node.GetAttribute("value")
						};
					}
					break;
				case ("WRITETEMPLATE"):
					{
						toret = new WriteTemplateStep
						{
							Help = node.GetAttribute("help"),
							To = node.GetAttribute("to"),
							Template = node.GetAttribute("template")
						};
					}
					break;
				case ("PARAM"):
					{
						toret = new ParamStep
						{
							Name = node.GetAttribute("name"),
							Value = node.GetAttribute("value")
						};
					}
					break;
				case ("SWITCH"):
					{
						toret = new SwitchWorkflowStep
						{
							Value = node.GetAttribute("value")
						};
					}
					break;
				case ("CASE"):
					{
						toret = new CaseWorkflowStep
						{
							Value = node.GetAttribute("value")
						};
					}
					break;
				case ("INCLUDE"):
					{
						toret = new IncludeStep();
					}
					break;
				default:
					foreach (var factory in _factories)
					{
						toret = factory.Value.Create(workflowType, node);
						if (toret != null) break;
					}
					break;
			}

			return toret;
		}
	}
}
