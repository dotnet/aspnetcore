@echo off
cd %~dp0

SETLOCAL
SET CACHED_NUGET="%LocalAppData%\NuGet\NuGet.exe"

IF EXIST %CACHED_NUGET% goto copynuget
echo Downloading latest version of NuGet.exe...
IF NOT EXIST "%LocalAppData%\NuGet" md "%LocalAppData%\NuGet"
@powershell -NoProfile -ExecutionPolicy unrestricted -Command "$ProgressPreference = 'SilentlyContinue'; Invoke-WebRequest 'https://www.nuget.org/nuget.exe' -OutFile '%CACHED_NUGET%'"

:copynuget
IF EXIST .nuget\nuget.exe goto build
md .nuget
copy %CACHED_NUGET% .nuget\nuget.exe > nul

:build
.nuget\NuGet.exe install Sake -ExcludeVersion -Source https://www.nuget.org/api/v2/ -Out packages
packages\Sake\tools\Sake.exe -I build -f makefile.shade %*
