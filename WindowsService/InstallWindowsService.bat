@ECHO OFF
SET THIS_DIR=%CD%
WindowsServiceSetup.exe -installer "Test Windows Service" -destination %CD%\Installed\WindowsService