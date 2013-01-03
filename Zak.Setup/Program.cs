using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Zak.Setup.Commons;
using Zak.Setup.Core.Setup;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;

/*
    Click Start, and then click Run.
    In the Open box, type regedt32, and then click OK.
    In Registry Editor, locate the following registry key:
    HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall
    In the left pane, click the Uninstall registry key, and then click Export on the File menu.
    In the Export Registry File dialog box that appears, click Desktop in the Save in list, type uninstall in the File name box, and then click Save.
    Each key listed under Uninstall in the left pane of Registry Editor represents a program that is displayed in the Currently installed programs list of the Add or Remove Programs tool. To determine which program that each key represents, click the key, and then view the following values in the details pane on the right:
    DisplayName: The value data for the DisplayName key is the name that is listed in Add or Remove Programs.

    -and-

    UninstallString: The value data for the UninstallString key is the program that is used to uninstall the program.
    After you identify the registry key that represents the program that you removed but which is still displayed in the Currently installed programs list of Add or Remove Programs, right-click the key in the left pane of the Registry Editor window, and then click Delete.

    Click Yes in response to the "Are you sure you want to delete this key and all of its subkeys?" message.
    On the File menu, click Exit to quit Registry Editor.
    Click Start, click Control Panel, and then click Add or Remove Programs.

    In the Currently installed programs list, verify that the program whose registry key you deleted is no longer listed.
    Do one of the following:
        If the program list is not correct in Add or Remove Programs, double-click the Uninstall.reg file that you saved to your desktop in step 5 to restore the original list of programs in the registry.

        -or-
        If the program list is correct in Add or Remove Programs, right-click the Uninstall.reg file on your desktop, and then click Delete.*/
namespace Zak.Setup
{
	class Program
	{
		private const string HELP_MESSAGE =
@"Execute the given template to create a new config file

Zak.Setup -destination [destination path] -template [template path] (-unattended) 
    -installer [installer name] -plugins [standard plugins] -rollback [rollbackFile]
Zak.Setup -help

  -destination   The destination folder
  -template      Set the template used to generate the setup wizard
  -unattended    Set if the setup is unattended, default to false
  -installer     Set the name of the installer on the 'add-remove programs'
  -help          Show this help
  -plugins       The root directory with the standard setup plugins
  -rollback		   Where should be stored the rollback script
  

Note that:
  The config file will be overwritten.
";



		static void Main(string[] args)
		{
			
			Console.ReadLine();
			var commandParser = new CommandParser(args, HELP_MESSAGE);

			if (commandParser.IsSet("runas"))
			{
				commandParser.RunAsAdmin();
				return;
			}

			if (commandParser.Has("uninstall", "rollback", "installer"))
			{

				var unattended = commandParser.IsSet("unattended");
				var undoPath = commandParser["rollback"];
				var setupName = commandParser["installer"];
				string pluginDirs = null;
				if (commandParser.IsSet("plugins"))
				{
					pluginDirs = commandParser["plugins"];
				}

				SetupLoader.Uninstall(undoPath, unattended, setupName, pluginDirs);

				if (commandParser.Has("cleaner", "destination"))
				{
					var cleaner = commandParser["cleaner"];
					var destination = commandParser["destination"];
					var guid = commandParser["guid"];
					LoadAndRunCleaner(cleaner, destination, commandParser.IsSet("unattended"), guid);
				}
			}
			else if (!SetupConfiguration(commandParser))
			{
				commandParser.ShowHelp();
			}
			if (!commandParser.IsSet("unattended"))
			{
				Console.ReadLine();
			}
		}

		private static bool SetupConfiguration(CommandParser commandParser)
		{
			if (commandParser.Has("destination", "template", "setupName", "mainapplication"))
			{
				using (RegistryKey parent = Registry.CurrentUser.OpenSubKey(
									 @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall", true))
				{
					if (parent == null)
					{
						Console.WriteLine("Uninstall registry key not found.");
						return false;
					}
					try
					{
						string guidText = commandParser["setupName"];
						if (parent.OpenSubKey(guidText, true) != null)
						{
							Console.WriteLine("First uninstall '{0}'", guidText);
							return false;
						}
					}
					catch
					{
					}
				}
				Console.WriteLine("Setting up on {0}", commandParser["destination"]);
				Console.WriteLine("");
				string pluginDirs = null;
				if (commandParser.IsSet("plugins"))
				{
					pluginDirs = commandParser["plugins"];
				}
				string rollbackPath = Path.Combine(commandParser["destination"], "rollaback.ser");
				if (commandParser.IsSet("rollback"))
				{
					rollbackPath = commandParser["rollback"];
				}
				var unattended = commandParser.IsSet("unattended");

				string sourcePath = null;
				if (commandParser.IsSet("source"))
				{
					sourcePath = commandParser["source"];
				}
				bool asAdministrator = false;
				if (SetupLoader.Start(commandParser["template"], commandParser["destination"], pluginDirs, unattended,ref asAdministrator, 
															rollbackPath,sourcePath))
				{
					CreateUninstaller(commandParser["destination"], rollbackPath, commandParser["setupName"], 
						commandParser["mainapplication"], asAdministrator);
				}
			}
			return true;
		}

		[Flags]
		enum MoveFileFlags
		{
			MOVEFILE_REPLACE_EXISTING = 0x00000001,
			MOVEFILE_COPY_ALLOWED = 0x00000002,
			MOVEFILE_DELAY_UNTIL_REBOOT = 0x00000004,
			MOVEFILE_WRITE_THROUGH = 0x00000008,
			MOVEFILE_CREATE_HARDLINK = 0x00000010,
			MOVEFILE_FAIL_IF_NOT_TRACKABLE = 0x00000020
		}

		[DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		static extern bool MoveFileEx(string lpExistingFileName, string lpNewFileName,
			 MoveFileFlags dwFlags);

		private static void LoadAndRunCleaner(string cleaner, string destination, bool unattended, string guid)
		{
			var newFileName = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".exe");

			byte[] updaterBytes = File.ReadAllBytes(cleaner);

			File.WriteAllBytes(newFileName, updaterBytes);


			MoveFileEx(newFileName, null, MoveFileFlags.MOVEFILE_DELAY_UNTIL_REBOOT);

			var startInfo = new ProcessStartInfo(newFileName)
				{
					WindowStyle = ProcessWindowStyle.Hidden,
					Arguments = string.Format(@" ""{0}"" ", destination)
				};

			if (guid != null)
			{
				using (RegistryKey parent = Registry.CurrentUser.OpenSubKey(
					@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall", true))
				{
					if (parent == null)
					{
						Console.WriteLine("Uninstall registry key not found.");
					}
					else
					{
						try
						{
							parent.DeleteSubKeyTree(guid, false);
							parent.DeleteSubKey(guid, false);
						}
						catch (Exception ex)
						{
							Console.WriteLine(ex);
						}
					}
				}
			}

			if (!unattended)
			{
				Console.WriteLine("Press a key to continue");
				Console.ReadKey();
			}
			var process = Process.Start(startInfo);
			Environment.Exit(0);
		}


		private static bool CreateUninstaller(string destination, string rollbackPath, string setupName, string mainapplication, bool asAdministrator)
		{
			using (RegistryKey parent = Registry.CurrentUser.OpenSubKey(
									 @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall", true))
			{
				if (parent == null)
				{
					Console.WriteLine("Uninstall registry key not found.");
					return false;
				}
				try
				{
					RegistryKey key = null;

					try
					{
						string guidText = setupName;// Guid.NewGuid().ToString("B");
						key = parent.OpenSubKey(guidText, true) ??
									parent.CreateSubKey(guidText);

						if (key == null)
						{
							throw new Exception(String.Format("Unable to create uninstaller '{0}'", guidText));
						}

						Assembly assembly = Assembly.LoadFrom(Path.Combine(destination, mainapplication));

						Version v = assembly.GetName().Version;

						var assemblyDescriptionAttribute = (AssemblyDescriptionAttribute)assembly.GetCustomAttributes(
							typeof(AssemblyDescriptionAttribute), false).FirstOrDefault();

						var assemblyDescriptionCopyright = (AssemblyCopyrightAttribute)assembly.GetCustomAttributes(
							typeof(AssemblyCopyrightAttribute), false).FirstOrDefault();

						var assemblyCompany = (AssemblyCompanyAttribute)assembly.GetCustomAttributes(
							typeof(AssemblyCompanyAttribute), false).FirstOrDefault();

						//asm.GetCustomAttributes()

						//asm.GetName().co

						key.SetValue("DisplayName", setupName);
						key.SetValue("ApplicationVersion", v.ToString());
						key.SetValue("DisplayVersion", v.ToString(3));
						key.SetValue("InstallDate", DateTime.Now.ToString("yyyy-MM-dd"));

						if (assemblyDescriptionAttribute != null) key.SetValue("Description", assemblyDescriptionAttribute.Description);
						if (assemblyDescriptionCopyright != null) key.SetValue("Copyright", assemblyDescriptionCopyright.Copyright);
						if (assemblyCompany != null) key.SetValue("Publisher", assemblyCompany.Company);


						key.SetValue("DisplayIcon", Path.Combine(destination, mainapplication));

						/*key.SetValue("URLInfoAbout", "http://www.blinemedical.com");
						key.SetValue("Contact", "support@mycompany.com");*/
						//%THIS_DIR%\uninstall\setup\Zak.Setup.exe -installer "Test Console App" -rollback %THIS_DIR%\uninstall\rollback.ser -plugins %THIS_DIR%\uninstall\plugins -uninstall -cleaner %THIS_DIR%\uninstall\setup\Zak.Setup.CleanUp.exe -destination %THIS_DIR%

						var uninstallPath = Path.Combine(destination, "uninstall");
						var setupPath = Path.Combine(uninstallPath, "setup", "Zak.Setup.exe");
						var pluginsPath = Path.Combine(uninstallPath, "plugins");
						var cleaner = Path.Combine(uninstallPath, "setup", "Zak.Setup.CleanUp.exe");
						var uninstallString = string.Format("{0} -uninstall -installer \"{1}\" -rollback \"{2}\" -plugins \"{3}\"" +
							" -cleaner \"{4}\" -destination \"{5}\" -guid \"{6}\"",
							setupPath, setupName, rollbackPath, pluginsPath, cleaner, destination, guidText);
						if (asAdministrator == true)
						{
							uninstallString += " -runas ";
						}
						key.SetValue("UninstallString", uninstallString);
					}
					finally
					{
						if (key != null)
						{
							key.Close();
						}
					}
				}
				catch (Exception ex)
				{
					Console.WriteLine(
							"An error occurred writing uninstall information to the registry.  The service is fully installed but can only be uninstalled manually through the command line.\n{0}",
							ex);
					return false;
				}
				return true;
			}
		}
	}
}
