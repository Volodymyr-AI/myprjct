@echo off
SET SERVICE_NAME=PMSIntegration
SET DISPLAY_NAME="PMS Integration Service"
SET DESCRIPTION="Service for integrating PMS data with DRPMS system"
SET EXE_PATH="%~dp0PMSIntegration.Worker.exe"

sc create %SERVICE_NAME% binPath= %EXE_PATH% start= auto DisplayName= %DISPLAY_NAME%
sc description %SERVICE_NAME% %DESCRIPTION%
sc start %SERVICE_NAME%
pause