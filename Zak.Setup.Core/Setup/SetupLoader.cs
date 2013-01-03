using System;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Authentication;
using Zak.Setup.Commons;
using Zak.Setup.Steps;

namespace Zak.Setup.Core.Setup
{
	public class SetupLoader
	{
		private static SetupFile _setupFile;
		private static string _templatesDirectory;
		private static string _destinationPath;

		public static bool Uninstall(string rollBackPath, bool unattended, string setupName, string pluginDirs)
		{
			string template = string.Empty;
			
			if (!string.IsNullOrWhiteSpace(pluginDirs))
			{
				AssembliesManager.LoadAssembliesFrom(pluginDirs);
			}
			_setupFile = DeserializeRollback(rollBackPath);
			_setupFile.PluginsDir = pluginDirs;
			
			SingleWorkflowStep.Initialize(_setupFile);
			_setupFile.Undoing = true;
			_setupFile.Unattended = unattended;
			Console.WriteLine("Uninstall started.");

			/*for (int i = (_setupFile.Rollback.Count - 1); i >= 0; i--)
			{
				if (_setupFile.Rollback[i].NeedAdminRights)
				{
					throw new AuthenticationException();
				}
			}*/
			for (int i = (_setupFile.Rollback.Count - 1); i >= 0; i--)
			{
				SingleWorkflowStep.ShowElementHelp(_setupFile.Rollback[i]);
				_setupFile.Rollback[i].Execute(ref template);
				_setupFile.Rollback[i].RunSteps(_setupFile.Rollback[i].WorkflowSteps, ref template);
				_setupFile.Rollback.RemoveAt(i);
			}
			Console.WriteLine("Uninstall completed.");
			return true;
		}

		public static bool Start(string templatePath, string destinationPath, string pluginDirs, bool unattended, 
			ref bool asAdministrator,
			string rollBackPath = null, string sourcePath=null)
		{
			_destinationPath = destinationPath.TrimEnd(Path.DirectorySeparatorChar);
			_templatesDirectory = Path.GetDirectoryName(templatePath);
			_setupFile = TemplateFileLoader.LoadTemplateFile(templatePath,pluginDirs, unattended);
			_setupFile.SetKey("${SetupDirectory}",Directory.GetCurrentDirectory());
			_setupFile.SetKey("${TemplatesDirectory}", _templatesDirectory);
			_setupFile.SetKey("${TemplatePath}", templatePath);
			_setupFile.SetKey("${DestinationPath}", _destinationPath);
			if (string.IsNullOrEmpty(sourcePath))
			{
				sourcePath = _templatesDirectory;
			}
			_setupFile.SetKey("${SourcePath}", sourcePath);
#if DEBUG
			_setupFile.SetKey("${Build}", "Debug");
#else
			_setupFile.SetKey("${Build}", "Release");
#endif
			return RunSetup(rollBackPath,ref asAdministrator);
		
		}

		#region Apply template

		#endregion

		#region Run setup region


		private static bool RunSetup(string rollBackPath, ref bool asAdministrator)
		{
			string template = string.Empty;
			try
			{
				Console.WriteLine("Setup started.");
				_setupFile.WorkflowRoot.Execute(ref template);
				Console.WriteLine("Setup completed.");
				SerializeRollback(rollBackPath,ref asAdministrator);
				return true;
			}
			catch (Exception ex)
			{
				_setupFile.Undoing = true;
				Console.WriteLine(ex.Message);
				Console.WriteLine("Rollback started.");
				for (int i = (_setupFile.Rollback.Count - 1); i >= 0; i--)
				{
					SingleWorkflowStep.ShowElementHelp(_setupFile.Rollback[i]);
					_setupFile.Rollback[i].Execute(ref template);
					_setupFile.Rollback[i].RunSteps(_setupFile.Rollback[i].WorkflowSteps,ref template);
					_setupFile.Rollback.RemoveAt(i);
				}
				Console.WriteLine("Rollback completed.");
			}
			return false;
		}

		private static void SerializeRollback(string rollBackPath, ref bool asAdministrator)
		{
			asAdministrator = false;
			if (string.IsNullOrWhiteSpace(rollBackPath))
			{
				rollBackPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
				if (string.IsNullOrWhiteSpace(rollBackPath))
				{
					throw new ArgumentException("Error writing undo");
				}
				rollBackPath = Path.Combine(rollBackPath, "undo.ser");
			}

			foreach (var step in _setupFile.Rollback)
			{
				if (step.NeedAdminRights) asAdministrator = true;
			}
			var binaryFormatter = new BinaryFormatter();
			using (var writeStream = new FileStream(rollBackPath,FileMode.Create,FileAccess.Write,FileShare.None))
			{
				binaryFormatter.Serialize(writeStream, _setupFile);
				writeStream.Close();
			}
		}

		private static SetupFile DeserializeRollback(string rollBackPath)
		{
			if (string.IsNullOrWhiteSpace(rollBackPath)) throw new FileNotFoundException("Missing rollback file",rollBackPath);
			var binaryFormatter = new BinaryFormatter();
			binaryFormatter.Binder = new DeserializeWithoutVersionBinder();
			SetupFile resultingObject;
			using (var readStream = new FileStream(rollBackPath, FileMode.Open, FileAccess.Read, FileShare.None))
			{
				resultingObject = binaryFormatter.Deserialize(readStream) as SetupFile;
				readStream.Close();
				if(resultingObject==null) throw new ArgumentException("Wrong serialized rollback sequence format");
			}
			return resultingObject;
		}

		#endregion
	}
}
