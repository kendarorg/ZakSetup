@echo off
CD ..
SET ROOT_DIR=%CD%
CD Setup

CD Installer
SET ELEMENT_ROLE=Receiver

SET CONFIG_DIR=%ROOT_DIR%\setup\Installer\Configs
SET SETUP_DIR=%ROOT_DIR%\setup\Sources

Mkdir "%ROOT_DIR%\VSM"
Mkdir "%ROOT_DIR%\VSM\%ELEMENT_ROLE%"
Mkdir "%ROOT_DIR%\VSM\%ELEMENT_ROLE%Files"

SET EXE_DIR=%ROOT_DIR%\VSM\%ELEMENT_ROLE%

COPY "%SETUP_DIR%\*.*" "%EXE_DIR%" /Y

ECHO %FileRoot%

call Zak.VSM.Setup -template "%CONFIG_DIR%\setup.xml" -destination "%EXE_DIR%" 
REM -i -spath "%EXE_DIR%\Zak.VSM.WinService.exe" -sname deltatreVSM%ELEMENT_ROLE% -sstart Manual
CD ..