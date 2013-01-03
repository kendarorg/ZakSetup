using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Xml;
using Zak.Setup.Commons;
using Zak.Setup.Enums;
using Zak.Setup.Factories;
using Zak.Setup.Setup;
using Zak.Setup.Steps;


namespace Zak.Setup.Core.Setup
{
	public class TemplateFileLoader
	{
		private static SetupFile _setupFile;
		private static string _templatesDirectory;
		private static XmlDocument _xmlDoc;
		private static List<string> _templatesDirectories = new List<string>();
		internal static SetupFile LoadTemplateFile(string templatePath, string pluginDirs, bool unattended)
		{
			_setupFile = new SetupFile(unattended);
			_setupFile.PluginsDir = pluginDirs;
			if (!string.IsNullOrWhiteSpace(_setupFile.PluginsDir))
			{
				AssembliesManager.LoadAssembliesFrom(_setupFile.PluginsDir);
			}
			SingleWorkflowStep.Initialize(_setupFile);
			_templatesDirectory = Path.GetDirectoryName(templatePath);
			_templatesDirectories.Add(_templatesDirectory);
			_xmlDoc = new XmlDocument(); //* create an xml document object.
			_xmlDoc.Load(templatePath); //* load the XML document from the specified file.

			/*var rootNode = _xmlDoc.GetElementsByTagName("root")[0];
			var rootConfig = rootNode.GetChildrenByTag("configs")[0];
			ExtractConfigs(rootConfig);

			var rootTemplates = rootNode.GetChildrenByTag("templates")[0];
			ExtractTemplates(rootTemplates);
			var baseTemplates = rootNode.GetChildrenByTag("baseTemplates")[0];
			ExtractBaseTemplates(baseTemplates);

			var workflow = rootNode.GetChildrenByTag("workflow")[0];
			ExtractWrokflow(workflow, _setupFile.WorkflowRoot);*/
			LoadRelativeXmlTemplate(_setupFile.WorkflowRoot, _xmlDoc);
			return _setupFile;
		}

		private static void ExtractWrokflow(XmlNode workflowRoot, SingleWorkflowStep root)
		{
			var workflowSteps = workflowRoot.GetChildrenByTag(new List<string>(_availableStepsSpecs.Keys).ToArray());
			for (int i = 0; i < workflowSteps.Count; i++)
			{
				var workflowStep = workflowSteps[i];
				var newStep = SetupStepFactory.CreateStep(workflowStep);
				if (workflowStep.GetAttribute("include") != null)
				{
					LoadExternalWorkflowSteps(workflowStep.GetAttribute("include"), newStep, workflowStep.GetAttribute("assemblyPath"));
				}
				ExtractWrokflow(workflowStep, newStep);
				root.WorkflowSteps.Add(newStep);
			}
		}


		private static void LoadExternalWorkflowSteps(string path, SingleWorkflowStep root, string assemblyPath)
		{
			string externalSteps;
			if (path.StartsWith("res:"))
			{
				externalSteps = LoadAssemblyResource(path, assemblyPath);
			}
			else
			{
				_templatesDirectories.Add(path);
				externalSteps = File.ReadAllText(path);
			}
			var xmlDoc = new XmlDocument(); //* create an xml document object.
			xmlDoc.LoadXml(externalSteps); //* load the XML document from the specified file.
			/*var workflow = xmlDoc.GetElementsByTagName("workflow")[0];
			ExtractWrokflow(workflow, root);*/
			LoadRelativeXmlTemplate(root, xmlDoc);
		}

		private static string LoadAssemblyResource(string path, string assemblyPath)
		{
			
			path = path.Substring(4);
			if (!string.IsNullOrEmpty(assemblyPath))
			{
				var asm = Assembly.ReflectionOnlyLoad(assemblyPath);
				return ExtractResourcesFromAGivenAssembly(path, asm);
			}
			Assembly[] asms = AppDomain.CurrentDomain.GetAssemblies();

			foreach (var asm in asms)
			{
				try
				{
					return ExtractResourcesFromAGivenAssembly(path, asm);
				}
				catch (Exception)
				{
					
				}
			}
			return null;
		}

		private static string ExtractResourcesFromAGivenAssembly(string path, Assembly asm)
		{
			string externalSteps = null;
			string resourceName = null;
			foreach (var res in asm.GetManifestResourceNames())
			{
				if (res.EndsWith(path, StringComparison.InvariantCultureIgnoreCase))
				{
					resourceName = res;
					break;
				}
			}
			if (resourceName == null)
			{
				throw new MissingManifestResourceException(path);
			}
			using (var stream = asm.GetManifestResourceStream(resourceName))
			{
				if (stream != null)
				{
					using (var reader = new StreamReader(stream))
					{
						externalSteps = reader.ReadToEnd();
					}
				}
			}
			return externalSteps;
		}

		private static void LoadRelativeXmlTemplate(SingleWorkflowStep root, XmlDocument xmlDoc)
		{
			var rootNode = xmlDoc.GetElementsByTagName("root")[0];

			var rootDll = rootNode.GetChildrenByTag("dlls");
			if (rootDll.Count == 1)
			{
				var rootDlls = rootNode.GetChildrenByTag("dlls")[0];
				ExtractDlls(rootDlls);
			}

			var rootConfig = rootNode.GetChildrenByTag("configs");
			if (rootConfig.Count == 1)
			{
				var rootConfigs = rootNode.GetChildrenByTag("configs")[0];
				ExtractConfigs(rootConfigs);	
			}
			
			var rootTemplates = rootNode.GetChildrenByTag("templates");
			if(rootTemplates.Count ==1)
			{
				var rootTemplate = rootTemplates[0];
				ExtractTemplates(rootTemplate);	
			}
			var baseTemplates = rootNode.GetChildrenByTag("baseTemplates");
			if (baseTemplates.Count == 1)
			{
				var baseTemplate = rootNode.GetChildrenByTag("baseTemplates")[0];
				ExtractBaseTemplates(baseTemplate);	
			}

			var workflows = rootNode.GetChildrenByTag("workflow");
			if (workflows.Count == 1)
			{
				var workflow = workflows[0];
				ExtractWrokflow(workflow, root);	
			}
		}

		

		#region Loading Available Configs

		private static void ExtractConfigs(XmlNode rootConfig)
		{
			var configs = rootConfig.GetChildrenByTag("config");
			for (int i = 0; i < configs.Count; i++)
			{
				var config = configs[i];
				ConfigTypes configType;
				if (!ConfigTypes.TryParse(config.GetAttribute("type"), true, out configType))
				{
					configType = ConfigTypes.Value;
				}

				var setupConfig = new SetupConfig
				{
					Key = config.GetAttribute("key"),
					AllowNone = config.GetAttributeBool("allownone"),
					ConfigType = configType,
					Help = config.GetAttribute("help"),
					Default = config.GetAttribute("default")
				};
				ExtractAvailableConfigs(config, setupConfig);
				_setupFile.SetupConfigs.Add(setupConfig.Key, setupConfig);
			}

			AddYesNoConfig();
		}

		private static void AddYesNoConfig()
		{
			var yesNoConfig = new SetupConfig
				{
					Key = "YesNo",
					AllowNone = false,
					ConfigType = ConfigTypes.Value,
					Help = "Answer Yes or No",
					Default = "0"
				};

			yesNoConfig.SetupChoices.Add(new SetupAvailable
				{
					Value = "false",
					Help = null,
					IsNull = false
				});

			yesNoConfig.SetupChoices.Add(new SetupAvailable
			{
				Value = "true",
				Help = null,
				IsNull = false
			});

			if (!_setupFile.SetupConfigs.ContainsKey(yesNoConfig.Key))
			{
				_setupFile.SetupConfigs.Add(yesNoConfig.Key, yesNoConfig);
			}
		}

		private static void ExtractAvailableConfigs(XmlNode config, SetupConfig setupConfig)
		{
			var availables = config.GetChildrenByTag("available");
			for (int i = 0; i < availables.Count; i++)
			{
				var available = availables[i];

				var availableConfig = new SetupAvailable
				{
					Value = available.GetAttribute("value"),
					Help = available.GetAttribute("help"),
					IsNull = false
				};

				ExtractWrokflow(available, availableConfig);
				setupConfig.SetupChoices.Add(availableConfig);
			}
			AddEmptyChoice(setupConfig);
		}

		private static void AddEmptyChoice(SetupConfig setupConfig)
		{
			if (setupConfig.AllowNone)
			{
				var availableConfig = new SetupAvailable
				{
					Value = "None",
					Help = "None of the above",
					IsNull = true
				};
				setupConfig.SetupChoices.Add(availableConfig);
			}
		}

		#endregion



		#region Templates
		private static void ExtractBaseTemplates(XmlNode baseTemplates)
		{
			var templates = baseTemplates.GetChildrenByTag("baseTemplate");
			for (int i = 0; i < templates.Count; i++)
			{
				var template = templates[i];

				var templateFile = template.GetAttribute("templateFile");
				string templateFileContent = null;
				if (templateFile != null)
				{
					if (templateFile.StartsWith("res:"))
					{
						templateFileContent = LoadAssemblyResource(templateFile, template.GetAttribute("assemblyPath"));
					}
					else
					{
						templateFile = Path.Combine(_templatesDirectory, templateFile);
						if (File.Exists(templateFile))
						{
							templateFileContent = File.ReadAllText(templateFile);
						}
					}
				}
				if (templateFileContent == null)
				{
					templateFileContent = template.InnerText;
				}
				templateFileContent = templateFileContent.Trim('\r', '\n', '\f') + "\n";
				if (templateFile != null && File.Exists(templateFile))
				{
					templateFileContent = File.ReadAllText(templateFile).Trim('\r', '\n', '\f') + "\n";
				}
				var setupTemplate = new SetupBaseTemplate
				{
					Content = templateFileContent,
					Id = template.GetAttribute("id"),
					TemplateSourceFile = template.GetAttribute("templateFile"),
					IsXml = template.GetAttributeBool("isxml")
				};
				_setupFile.BaseTemplates.Add(setupTemplate.Id, setupTemplate);
			}
		}

		private static void ExtractTemplates(XmlNode rootTemplates)
		{
			var templates = rootTemplates.GetChildrenByTag("template");
			for (int i = 0; i < templates.Count; i++)
			{
				var template = templates[i];

				var setupTemplate = new SetupTemplate
				{
					Content = template.InnerText.Trim('\r', '\n', '\f') + "\n",
					Id = template.GetAttribute("id")
				};
				_setupFile.SetupTemplates.Add(setupTemplate.Id, setupTemplate);
			}
		}
		#endregion

		private static Dictionary<string, SingleWorkflowStep> _availableStepsSpecs;

		private static void ExtractDlls(XmlNode rootDlls)
		{
			_availableStepsSpecs = new Dictionary<string, SingleWorkflowStep>();
			var dlls = rootDlls.GetChildrenByTag("dll");

			for (int i = 0; i < dlls.Count; i++)
			{
				var listOfAvailablePaths = new List<string>(_templatesDirectories.ToArray());
				
				var dll = dlls[i];
				var location = dll.GetAttribute("location");
				var dllName = dll.GetAttribute("name");

				listOfAvailablePaths.Add(location);
				listOfAvailablePaths.Add(Environment.CurrentDirectory);
				listOfAvailablePaths.Add(Environment.SystemDirectory);
				listOfAvailablePaths.Add(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location));

				if (!AssembliesManager.LoadAssemblyFrom(dllName, null,
					listOfAvailablePaths.ToArray()))
				{
					throw new FileNotFoundException("Impossible to load assembly", dllName);
				}
			}

			var loaders = new List<Type>(AssembliesManager.LoadTypesInheritingFrom(typeof(IOnLoad))).ToArray();
			foreach (var loader in loaders)
			{
				var step = (IOnLoad)Activator.CreateInstance(loader);
				step.Initialize();
			}

			var files = new List<Type>( AssembliesManager.LoadTypesInheritingFrom(typeof (SingleWorkflowStep))).ToArray();
			foreach (var stepType in files)
			{
				var step = (SingleWorkflowStep) Activator.CreateInstance(stepType);
				if (step.GetNodeType()!=null)
				{
					if (!_availableStepsSpecs.ContainsKey(step.GetNodeType()))
					{
						_availableStepsSpecs.Add(step.GetNodeType(), step);
					}
				}
			}

			var factories = new List<Type>(AssembliesManager.LoadTypesInheritingFrom(typeof(BaseStepFactory))).ToArray();
			foreach (var factory in factories)
			{
				var stepFactory = (BaseStepFactory)Activator.CreateInstance(factory);
				SetupStepFactory.AddFactory(stepFactory);
			}
		}
	}
}
