@echo off
CD ..
SET ROOT_DIR=%CD%
CD Setup

CD Installer
SET ELEMENT_ROLE=Sender
SET ROOT_DIR=C:\Tmp

SET SETUP_DIR=%ROOT_DIR%\Setup

SET EXE_DIR=%ROOT_DIR%\VSM\%ELEMENT_ROLE%


Zak.VSM.Setup -u -sname deltatreVSM%ELEMENT_ROLE%
Cd ..