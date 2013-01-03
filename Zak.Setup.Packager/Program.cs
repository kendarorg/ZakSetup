using System;
using System.Collections.Generic;
using System.Text;
using Zak.Setup.Commons;
using System.IO;
using Ionic.Zip;
using System.Reflection;

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
		static void Main(string[] args)
		{
			var cp = new CommandParser(args,HELP);
			if (cp.Has("setup", "application", "outcome", "script", "mainapplication"))
			{
				var starterScript = Path.GetFullPath(cp["script"]);
				var setupDir = Path.GetFullPath(cp["setup"]);
				var applicationDir = Path.GetFullPath(cp["application"]);
				var applicationDirPlugins = Path.Combine(Path.GetFullPath(cp["application"]),"Plugins");
				var outcomeFile = Path.GetFullPath(cp["outcome"]);
				var setupCoreDir = Path.Combine(setupDir, "Core");
				var setupPackagerDir = Path.Combine(setupDir, "Packager");
				var setupRunnerDir = Path.Combine(setupDir, "Runner");
				var setupPluginsDir = Path.Combine(setupDir, "Plugins");

				var tmpPath =  Path.GetTempPath();
				var tmpDirectory = Path.Combine(tmpPath, Guid.NewGuid().ToString());
				Directory.CreateDirectory(tmpDirectory);

				try
				{
					var filesToAdd = new List<FileDescriptor>();
					filesToAdd.AddRange(SetupFileDescriptors(
						applicationDir,
						"application",
						Directory.GetFiles(cp["application"], "*.*", SearchOption.AllDirectories)));

					filesToAdd.AddRange(SetupFileDescriptors(
						setupCoreDir,
						"setup",
						Directory.GetFiles(setupCoreDir, "*.*", SearchOption.AllDirectories)));

					//Load application plugins
					if (Directory.Exists(applicationDirPlugins))
					{
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
							filesToAdd.AddRange(SetupFileDescriptors(
								singlePluginDir,
								"plugins" + Path.DirectorySeparatorChar + plugin,
								Directory.GetFiles(singlePluginDir, "*.*", SearchOption.AllDirectories)));
						}
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
					Console.WriteLine("CleanedUp temporary files.");
				}
			}
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

		private static IEnumerable<FileDescriptor> SetupFileDescriptors(string root,string extraZipDir,string[] getFiles)
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
	}
}
