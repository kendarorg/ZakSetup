using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using Zak.Setup.Commons;
using System.IO;
using Ionic.Zip;
using System.Reflection;
using Zak.Setup.Steps;

namespace Zak.Setup.Packager
{
	class Program
	{
		class FileDescriptor
		{
			public string SourcePath { get; set; }
			public string ZipPath { get; set; }

			public override string ToString()
			{
				return ZipPath;
			}
		}

		private const string HELP = "Help not present";
		private static string _mtExePath = null;
		static void Main(string[] args)
		{
			FileDescriptor temporaryManifestedExecutable = null;
			var cp = new CommandParser(args, HELP);
			if (cp.Has("setup", "application", "outcome", "script", "mainapplication"))
			{
				if (cp.IsSet("mt"))
				{
					_mtExePath = cp["mt"];
				}
				var starterScript = Path.GetFullPath(cp["script"]);
				var setupDir = Path.GetFullPath(cp["setup"]);
				var applicationDir = Path.GetFullPath(cp["application"]);
				var applicationDirPlugins = Path.Combine(Path.GetFullPath(cp["application"]), "Plugins");
				var outcomeFile = Path.GetFullPath(cp["outcome"]);
				var setupCoreDir = Path.Combine(setupDir, "Core");
				var setupPackagerDir = Path.Combine(setupDir, "Packager");
				var setupRunnerDir = Path.Combine(setupDir, "Runner");
				var setupPluginsDir = Path.Combine(setupDir, "Plugins");

				var tmpPath = Path.GetTempPath();
				var tmpDirectory = Path.Combine(tmpPath, Guid.NewGuid().ToString());
				Directory.CreateDirectory(tmpDirectory);

				try
				{
					var filesToAdd = new List<FileDescriptor>();
					var pluginsDirs = new List<string>();

					//Load application plugins
					if (Directory.Exists(applicationDirPlugins))
					{
						pluginsDirs.Add(applicationDirPlugins);
						var applicationPlugins = Directory.GetDirectories(applicationDirPlugins, "*.*", SearchOption.TopDirectoryOnly);

						foreach (var singlePluginDir in applicationPlugins)
						{
							var plugin = Path.GetFileName(singlePluginDir);
							filesToAdd.AddRange(SetupFileDescriptors(
								singlePluginDir,
								"plugins" + Path.DirectorySeparatorChar + plugin,
								Directory.GetFiles(singlePluginDir, "*.*", SearchOption.AllDirectories)));
						}
					}

					if (cp.IsSet("plugins"))
					{
						var pluginString = cp["plugins"];
						var plugins = pluginString.Split(';');
						foreach (var plugin in plugins)
						{
							var singlePluginDir = Path.Combine(setupPluginsDir, plugin);
							pluginsDirs.Add(singlePluginDir);
							filesToAdd.AddRange(SetupFileDescriptors(
								singlePluginDir,
								"plugins" + Path.DirectorySeparatorChar + plugin,
								Directory.GetFiles(singlePluginDir, "*.*", SearchOption.AllDirectories)));
						}
					}

					var manifestNeeded = CheckManifestNeed(pluginsDirs);


					filesToAdd.AddRange(SetupFileDescriptors(
						applicationDir,
						"application",
						Directory.GetFiles(cp["application"], "*.*", SearchOption.AllDirectories)));

					filesToAdd.AddRange(SetupFileDescriptors(
						setupCoreDir,
						"setup",
						Directory.GetFiles(setupCoreDir, "*.*", SearchOption.AllDirectories)));

					if (manifestNeeded > 0)
					{
						var removedSetup = RemoveSetupExeFromFileList(filesToAdd);
						temporaryManifestedExecutable = AddNewManifestedSetupExe(removedSetup, filesToAdd, manifestNeeded);
						filesToAdd.Add(temporaryManifestedExecutable);
					}

					var zipArchive = Path.Combine(tmpDirectory, "SetupContent.zip");
					using (var zip = new ZipFile())
					{
						for (int i = 0; i < filesToAdd.Count; i++)
						{
							var item = filesToAdd[i];
							zip.AddFile(item.SourcePath).FileName = item.ZipPath;
						}
						zip.Save(zipArchive);
						Console.WriteLine("Created temporary archive.");
					}


					var setupZipDll = Path.Combine(setupPackagerDir, "Ionic.Zip.dll");
					var runnerExecutable = Path.Combine(setupRunnerDir, "Zak.Setup.Runner.exe");

					var runnerExecutableBytes = File.ReadAllBytes(runnerExecutable);
					var zipDllBytes = File.ReadAllBytes(setupZipDll);
					var scriptBytes = File.ReadAllBytes(starterScript);
					var contentArchiveBytes = File.ReadAllBytes(zipArchive);
					var separator = ASCIIEncoding.ASCII.GetBytes("XXXTHISISTHESEPA@@@RATOR@@@ASFARASIKNOWSONO@@@PROBLEMXXX");

					var extraParameters = ASCIIEncoding.ASCII.GetBytes(string.Format("mainapplication={0}\n", cp["mainapplication"]));

					var ms = new MemoryStream();
					ms.Write(runnerExecutableBytes, 0, runnerExecutableBytes.Length);
					ms.Write(separator, 0, separator.Length);
					ms.Write(zipDllBytes, 0, zipDllBytes.Length);
					ms.Write(separator, 0, separator.Length);
					ms.Write(contentArchiveBytes, 0, contentArchiveBytes.Length);
					ms.Write(separator, 0, separator.Length);
					ms.Write(scriptBytes, 0, scriptBytes.Length);
					ms.Write(separator, 0, separator.Length);
					ms.Write(extraParameters, 0, extraParameters.Length);
					File.WriteAllBytes(outcomeFile, ms.ToArray());
					Console.WriteLine("Written setup {0}.", outcomeFile);
				}
				catch (Exception ex)
				{
					Console.WriteLine("Error:\n{0}.", ex.ToString());
					if (File.Exists(outcomeFile))
					{
						File.Delete(outcomeFile);
					}
				}
				finally
				{
					CleanUpDir(new DirectoryInfo(tmpDirectory));

					if (temporaryManifestedExecutable != null)
					{
						File.Delete(temporaryManifestedExecutable.SourcePath);
						File.Delete(temporaryManifestedExecutable.SourcePath+".Manifest");
						Directory.Delete(Path.GetDirectoryName(temporaryManifestedExecutable.SourcePath));
					}

					Console.WriteLine("CleanedUp temporary files.");
				}
			}
		}

		private static FileDescriptor RemoveSetupExeFromFileList(List<FileDescriptor> filesToAdd)
		{
			for (int i = filesToAdd.Count - 1; i >= 0; i--)
			{
				var fileToAdd = filesToAdd[i];
				if (fileToAdd.SourcePath.ToLowerInvariant().EndsWith("zak.setup.exe"))
				{
					filesToAdd.RemoveAt(i);
					return fileToAdd;
				}
			}
			return null;
		}

		private static int CheckManifestNeed(List<string> filesToAdd)
		{
			int level = 0;
			foreach (var item in filesToAdd)
			{
				var loadedAssemblies = AssembliesManager.LoadAssembliesFrom(item);
				foreach (var loadedAssembly in loadedAssemblies)
				{
					var loadedTypes = AssembliesManager.LoadTypesInheritingFrom(loadedAssembly, new Type[] { typeof(SingleWorkflowStep) });
					foreach (var loadedType in loadedTypes)
					{
						var swf = (SingleWorkflowStep)Activator.CreateInstance(loadedType);
						if (swf.NeedHighestAvailableRights && level == 0) level = 1;
						if (swf.NeedAdminRights && level <= 1) level = 2;
					}
				}
			}

			return level;
		}

		private static void CleanUpDir(DirectoryInfo rootDirInfo)
		{
			DirectoryInfo[] dirs = rootDirInfo.GetDirectories("*", SearchOption.TopDirectoryOnly);
			foreach (FileInfo file in rootDirInfo.GetFiles("*.*", SearchOption.TopDirectoryOnly))
			{
				file.Delete();
			}
			for (int index = dirs.Length - 1; index >= 0; index--)
			{
				CleanUpDir(dirs[index]);
			}
			Directory.Delete(rootDirInfo.FullName);
		}

		private static IEnumerable<FileDescriptor> SetupFileDescriptors(string root, string extraZipDir, string[] getFiles)
		{
			var toret = new List<FileDescriptor>();
			foreach (var singleFile in getFiles)
			{
				var file = Path.GetFullPath(singleFile);
				var zipPath = file.Replace(root, "").Trim(Path.DirectorySeparatorChar);
				if (!string.IsNullOrWhiteSpace(extraZipDir))
				{
					zipPath = extraZipDir + Path.DirectorySeparatorChar + zipPath;
				}
				toret.Add(new FileDescriptor
					{
						SourcePath = file,
						ZipPath = zipPath
					});
			}
			return toret;
		}

		private static FileDescriptor AddNewManifestedSetupExe(FileDescriptor removedSetup, List<FileDescriptor> filesToAdd, int manifestNeeded)
		{
			var manifestString = "asInvoker";
			if (manifestNeeded == 1) manifestString = "highestAvailable";
			if (manifestNeeded == 2) manifestString = "requireAdministrator";
#if DEBUG
			Console.WriteLine("Required execution rights: {0}",manifestString);
			var tmpPath = Path.Combine(@"C:\Projects\Zak.Setup\Install",Guid.NewGuid().ToString());// Path.GetTempPath();
#else
			var tmpPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
#endif


			var fileName = Path.GetFileName(removedSetup.SourcePath);
			Directory.CreateDirectory(tmpPath);
			var tmpFileName = Path.Combine(tmpPath, fileName);
			
			var tmpFileNameManifest = Path.Combine(tmpPath, fileName + ".manifest");
			File.Copy(removedSetup.SourcePath, tmpFileName);
			
			var tmpManifestContent = string.Format(TEMPLATE_FOR_MANIFEST, manifestString);
			File.WriteAllText(tmpFileNameManifest, tmpManifestContent);
			var startInfo = new ProcessStartInfo(Path.Combine(_mtExePath,"mt.exe"));
			
			startInfo.WindowStyle = ProcessWindowStyle.Hidden;
			
			startInfo.Arguments = string.Format(@" -manifest ""{0}"" ", tmpFileNameManifest);
			startInfo.Arguments += string.Format(@" -outputresource:""{0}"";#1 ", tmpFileName);
			var process = Process.Start(startInfo);
			
			while (!process.WaitForExit(100))
			{
				Thread.Sleep(100);
			}

#if DEBUG
			Console.WriteLine("Executing: {0} {1}", Path.Combine(_mtExePath, "mt.exe"), startInfo.Arguments);
#endif

			
			removedSetup.SourcePath = tmpFileName;
			return removedSetup;
		}

		private const string TEMPLATE_FOR_MANIFEST = @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
    <assembly xmlns=""urn:schemas-microsoft-com:asm.v1"" manifestVersion=""1.0"">
    <trustInfo xmlns=""urn:schemas-microsoft-com:asm.v3"">
        <security>
            <requestedPrivileges>
                <requestedExecutionLevel level=""{0}"" uiAccess=""false""/>
            </requestedPrivileges>
        </security>
    </trustInfo>
</assembly>";
	}
}
