To use the setup:

Install through nuget the package
=================================
	Zak.Setup.Core.Setup

And all the needed plugin e.g.
=================================
	Zak.Setup.Services

Add the setup.xml file into the project that will start the setup (this simply copy everything onto a directory)
=================================
<?xml version="1.0"?>
<root>
	<dlls/>
	<configs/>
	<templates/>
	<baseTemplates/>
	<workflow>
		<copy from="${SourcePath}" to="${DestinationPath}" what="*.dll;*.exe;*.bat"/>
	</workflow>
</root>

Prepare the post-build events to create the setup
=================================
REM These are standard
SET SETUP_CORE=$(SolutionDir).Zak.Setup
SET PACKAGER=%SETUP_CORE%\Packager\Zak.Setup.Packager.exe

REM Where should put the parts to install
SET APPLICATION_PATH=$(SolutionDir)TemporarySetup

REM What will be the executable from wich should take all informations, this MUST have all AssemblyInfo filled
SET MAIN_EXE_PATH=$(TargetPath)

REM The name of the setup that will be created
SET OUTCOME=$(SolutionDir)$(ProjectName)Setup.exe

REM The script for the setup
SET SCRIPT=$(ProjectDir)setup.xml

REM Prepare the destination directory
mkdir %APPLICATION_PATH%

REM Copy the dlls
copy $(TargetDir)*.dll %APPLICATION_PATH%\ /Y
REM Copy the executables
copy $(TargetDir)*.exe %APPLICATION_PATH% /Y
REM Copy the configurations
copy $(TargetDir)*.config %APPLICATION_PATH% /Y
REM Copy the setup script
copy %SCRIPT% %APPLICATION_PATH% /Y
REM Set the path for the windows SDK mt.exe to add the correct manifest (for uac) to the setup executable generated
SET MT_EXE_PATH=C:\Program Files\Microsoft SDKs\Windows\v7.1\Bin

REM Run the packager!!
"%PACKAGER%" -setup "%SETUP_CORE%" -application "%APPLICATION_PATH%" -outcome "%OUTCOME%" -script "%SCRIPT%" -mainapplication "%MAIN_EXE_PATH%" -mt "%MT_EXE_PATH%"

When willing to add plugins, the ones available at the moment are:
===================================================================
	Zak.Setup.Services
	Zak.Setup.IIS6  (not working)
	Zak.Setup.IIS7  (not working)
	Zak.Setup.BatchRunner  (not working)

To use them while running the packager a flag should be added:
 -plugins Services;IIS6;AnotherPlugin

The plugin naming convention use the last part of the plugin as plugin name!!

Each plugin is able to detect if it will need administrative or elevated rights to run!