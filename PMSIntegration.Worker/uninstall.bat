@echo off
SET SERVICE_NAME=PMSIntegration
sc stop %SERVICE_NAME%
sc delete %SERVICE_NAME%
pause