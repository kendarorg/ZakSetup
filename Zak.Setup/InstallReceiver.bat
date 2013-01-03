@echo off
CD ..
SET ROOT_DIR=%CD%
CD Setup

CD Installer
SET ELEMENT_ROLE=Receiver
SET ROOT_DIR=C:\Tmp

SET CONFIG_DIR=%ROOT_DIR%\setup\Installer\Configs
SET SETUP_DIR=%ROOT_DIR%\setup\Sources

Mkdir "%ROOT_DIR%\VSM"
Mkdir "%ROOT_DIR%\VSM\%ELEMENT_ROLE%"
Mkdir "%ROOT_DIR%\VSM\%ELEMENT_ROLE%Files"

SET EXE_DIR=%ROOT_DIR%\VSM\%ELEMENT_ROLE%

COPY "%SETUP_DIR%\*.*" "%EXE_DIR%" /Y


SET Role=1
SET BusService=1
SET BusTopics=1
SET Topic=testvsmtopic
SET SourceStorageService=2
SET DestinationStorageService=1
SET FileRoot=%ROOT_DIR%\VSM\%ELEMENT_ROLE%Files
SET BlobRoot=testvsmblob
SET ServiceBusConnectionString=default_value_applied
SET ServiceBusConnectionString=default_value_applied
SET StorageConnectionString=default_value_applied
SET FileWatcherService=2

ECHO %FileRoot%

call Zak.VSM.Setup -template "%CONFIG_DIR%\setup.xml" -destination "%EXE_DIR%" 
REM -i -spath "%EXE_DIR%\Zak.VSM.WinService.exe" -sname deltatreVSM%ELEMENT_ROLE% -sstart Manual
CD ..