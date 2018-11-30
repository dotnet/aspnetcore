@echo off
powershell -ExecutionPolicy ByPass -NoProfile -command "& """%~dp0Build.ps1""" -msbuildEngine dotnet -restore -execute /p:PublishBuildAssets=true /p:SdkTaskProjects=PublishBuildAssets.proj %*"
exit /b %ErrorLevel%
