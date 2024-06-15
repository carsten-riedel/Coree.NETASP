@echo off
echo Starting script execution...

:: Retrieve the top-level directory of the current git repository
for /f "delims=" %%i in ('git rev-parse --show-toplevel') do set "topLevelDirectory=%%i"

:: Change the current directory to the top-level directory of the git repository
cd %topLevelDirectory%
echo Changed to git top-level directory: %topLevelDirectory%

:: Section for installing tools and dependencies
echo --------------------------------------------
echo Installing PowerShell as a global .NET tool...
dotnet tool install --global PowerShell --version 7.4.1
echo PowerShell installation completed.

:: Execute PowerShell script
echo --------------------------------------------
echo Executing the PowerShell workflow script...
pwsh .github/workflows/build_deploy_new.ps1

:: End of script execution
echo --------------------------------------------
echo Script execution finished. Press any key to exit.
pause
