using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;

namespace Zak.Setup.Runner
{
	class Program
	{
		private static int GetPositionAfterMatch(byte[] data, byte[] pattern, int startat = 0)
		{
			for (int i = startat; i < data.Length - pattern.Length; i++)
			{
				bool match = true;
				for (int k = 0; k < pattern.Length; k++)
				{
					if (data[i + k] != pattern[k])
					{
						match = false;
						break;
					}
				}
				if (match)
				{
					return i + pattern.Length;
				}
			}
			return -1;
		}

		public static string Separator
		{
			get
			{
				string toret = "XXXTHISISTHESEPA";
				toret += "FUFFA";
				toret += "@@@RATOR@@@ASFARASIKNOWSONO@@@PROBLEMXXX";
				return toret.Replace("FUFFA", "");
			}
		}

		private static string _temporaryInstallDirectory;

		static void Main(string[] args)
		{
			if (!ParseArgs(args))
			{
				Console.WriteLine(HELP);
				return;
			}
			var thisLocation = Assembly.GetExecutingAssembly().Location;

#if DEBUG
			var tmpPath = @"C:\Projects\Zak.Setup\Install";// Path.GetTempPath();
#else
			var tmpPath = Path.GetTempPath();
#endif

			_temporaryInstallDirectory = Path.Combine(tmpPath, Guid.NewGuid().ToString());

			//var thisLocationDebug = Path.GetDirectoryName(thisLocation);
			//if (thisLocationDebug == null) return;
			//var tmpDirectory = Path.Combine(thisLocationDebug, "test");


			Directory.CreateDirectory(_temporaryInstallDirectory);
			try
			{
				var tmpContent = File.ReadAllBytes(thisLocation);
				var separator = ASCIIEncoding.ASCII.GetBytes(Separator);
				var zipDllPosition = GetPositionAfterMatch(tmpContent, separator);
				var contentPosition = GetPositionAfterMatch(tmpContent, separator, zipDllPosition);
				var setupScriptPosition = GetPositionAfterMatch(tmpContent, separator, contentPosition);
				var extraParametersPosition = GetPositionAfterMatch(tmpContent, separator, setupScriptPosition);

				byte[] zipDllBytes;
				using (var stream = new MemoryStream())
				{
					stream.Write(tmpContent, zipDllPosition, contentPosition - separator.Length - zipDllPosition);
					stream.Seek(0, SeekOrigin.Begin);
					zipDllBytes = stream.ToArray();
				}

				var asm = Assembly.Load(zipDllBytes);
				var typeZipFile = asm.GetType("Ionic.Zip.ZipFile");
				using (var stream = new MemoryStream())
				{
					//stream.Write(tmpContent, contentPosition, tmpContent.Length - contentPosition);
					stream.Write(tmpContent, contentPosition, setupScriptPosition - separator.Length - contentPosition);
					stream.Seek(0, SeekOrigin.Begin);
					using (var zipFile = (IDisposable)typeZipFile.InvokeMember(
						"Read", BindingFlags.InvokeMethod, null,
						typeZipFile, new object[] { stream }))
					{
						typeZipFile.InvokeMember(
							"ExtractAll", BindingFlags.InvokeMethod, null,
							zipFile, new object[] { _temporaryInstallDirectory });
					}
				}
				
				using (var stream = new MemoryStream())
				{
					stream.Write(tmpContent, setupScriptPosition,extraParametersPosition - separator.Length - setupScriptPosition);
					stream.Seek(0, SeekOrigin.Begin);
					File.WriteAllBytes(Path.Combine(_temporaryInstallDirectory, "application", "setup.xml"), stream.ToArray());
				}

				string extraParameters = null;
				if (extraParametersPosition > 0 && (tmpContent.Length - extraParametersPosition) > 0)
				{
					extraParameters = ASCIIEncoding.ASCII.GetString(tmpContent, extraParametersPosition, tmpContent.Length - extraParametersPosition);
				}

				ExecuteInstaller(extraParameters);
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.ToString());
			}
			CleanUpDir(new DirectoryInfo(_temporaryInstallDirectory));
			//if(!_unattended) Console.ReadKey();
		}

		private static void ExecuteInstaller(string extraParameters)
		{
			try
			{
				if (_installerName != null && _destination != null)
				{
					var setupPath = Path.Combine(_temporaryInstallDirectory, "setup", "Zak.Setup.exe");
					var pluginsRoot = Path.Combine(_temporaryInstallDirectory, "plugins");
					var scriptPath = Path.Combine(_temporaryInstallDirectory, "application", "setup.xml");
					if (_scriptFile != null)
					{
						scriptPath = _scriptFile;
					}
					
					if (_doInstallation)
					{
						Directory.CreateDirectory(_destination);
						Directory.CreateDirectory(Path.Combine(_destination,"uninstall"));
						var di = new DirectoryInfo(Path.Combine(_destination,"uninstall"));

						if ((di.Attributes & FileAttributes.Hidden) != FileAttributes.Hidden)
						{
							di.Attributes |= FileAttributes.Hidden;
						}

						Directory.CreateDirectory(Path.Combine(_destination,"uninstall","setup"));
						Directory.CreateDirectory(Path.Combine(_destination,"uninstall","plugins"));
					}

					var startInfo = new ProcessStartInfo(setupPath);

					if (_unattended)
					{
						startInfo.WindowStyle = ProcessWindowStyle.Hidden;
					}

					startInfo.Arguments = string.Format(@" -plugins ""{0}"" ", pluginsRoot);
					startInfo.Arguments += string.Format(@" -setupName ""{0}"" ", _installerName);
					startInfo.Arguments += string.Format(@" -destination ""{0}"" ", _destination);

					if (!string.IsNullOrWhiteSpace(extraParameters))
					{
						foreach (var extraParameter in extraParameters.Split('\n'))
						{
							if (extraParameter.Length > 1 && !string.IsNullOrWhiteSpace(extraParameter))
							{
								var kvp = extraParameter.Split('=');
								if (kvp.Length == 2) startInfo.Arguments += string.Format(@" -{0} ""{1}"" ", kvp[0], kvp[1]);
							}
						}
					}

					if (_unattended) startInfo.Arguments += " -unattended ";
					if (_doInstallation)
					{
						var rollBackPath = Path.Combine(_destination, "uninstall", "rollback.ser");
						startInfo.Arguments += string.Format(@" -rollback ""{0}"" ", rollBackPath);
						startInfo.Arguments += string.Format(@" -template ""{0}"" ", scriptPath);
						
						CopyAll(Path.Combine(_temporaryInstallDirectory, "setup"), Path.Combine(_destination, "uninstall", "setup"));
						CopyAll(Path.Combine(_temporaryInstallDirectory, "plugins"), Path.Combine(_destination, "uninstall", "plugins"));
					}
					else
					{
						startInfo.Arguments += " -uninstall ";
					}
					//startInfo.RedirectStandardError = true;
					//startInfo.RedirectStandardInput = true;
					//startInfo.RedirectStandardOutput = true;
					//startInfo.CreateNoWindow = true;
					//startInfo.UseShellExecute = false;
					
					var process = Process.Start(startInfo);
					while (!process.WaitForExit(100))
					{
						Thread.Sleep(100);
					}
					Console.WriteLine("Setup completed.");
				}
			}
			catch (Exception)
			{
				if (_doInstallation && _destination != null && Directory.Exists(_destination))
				{
					CleanUpDir(new DirectoryInfo(_destination));
				}
				throw;
			}
		}

		private static void CopyAll(string source, string dest)
		{
			
			if(dest[dest.Length-1]!=Path.DirectorySeparatorChar) dest+=Path.DirectorySeparatorChar;
			if (!Directory.Exists(source)) return;
			if(!Directory.Exists(dest)) Directory.CreateDirectory(dest);
			
			var files=Directory.GetFileSystemEntries(source);
			foreach(string element in files)
			{
				// Sub directories
				if (Directory.Exists(element))
				{
					CopyAll(element, dest + Path.GetFileName(element));
				}
				else
				{
					
					File.Copy(element, dest + Path.GetFileName(element), true);
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

		private static string _destination;
		private static string _installerName;
		private static string _scriptFile;
		private static bool _unattended = false;
		private static bool _doInstallation = false;

		private const string HELP = @"
-destination [destination dir]
-installer   [name on add remove program]
-uninstall																	 Just a flag to uninstall
-template	   [the xml script]
-unattended																	Flag for unattended setups
";

		private static bool ParseArgs(string[] args)
		{
			_doInstallation = true;
			for (int index = 0; index < args.Length; index++)
			{
				var arg = args[index];
				if (arg.StartsWith("-") && index < (args.Length - 1))
				{
					arg = arg.ToLowerInvariant();
					switch (arg)
					{
						case ("-destination"):
							_destination = args[index + 1];
							index++;
							break;
						case ("-installer"):
							_installerName = args[index + 1];
							index++;
							break;
						case ("-uninstall"):
							_doInstallation = false;
							break;
						case ("-template"):
							_scriptFile = args[index + 1];
							index++;
							break;
						case ("-unattended"):
							_unattended = true;
							break;
						default:
							return false;
					}
				}
				else if (arg.StartsWith("-"))
				{
					arg = arg.ToLowerInvariant();
					switch (arg)
					{
						case ("-uninstall"):
							_doInstallation = false;
							break;
						case ("-unattended"):
							_unattended = true;
							break;
						default:
							return false;
					}
				}
				else
				{
					return false;
				}
			}
			if (!_unattended && string.IsNullOrEmpty(_destination))
			{
				Console.WriteLine("Set Destination Directory:");
				_destination = Console.ReadLine().Trim();
			}

			if (!_unattended && string.IsNullOrEmpty(_installerName))
			{
				Console.WriteLine("Set Installer Name:");
				_installerName = Console.ReadLine().Trim();
			}

			if (_doInstallation)
			{
				if (_installerName == null)
				{
					_installerName = Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly().Location);
				}
			}
			
			if (string.IsNullOrWhiteSpace(_installerName) || string.IsNullOrWhiteSpace(_destination ) || (_scriptFile == null && !_doInstallation))
			{
				return false;
			}

			return true;
		}
	}
}
