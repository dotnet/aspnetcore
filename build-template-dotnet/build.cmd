@echo off
cd %~dp0

SETLOCAL
SET REPO_FOLDER=%CD%
SET DOTNET_INSTALL_DIR=%REPO_FOLDER%\packages

SET NUGET_VERSION=latest
SET CACHED_NUGET=%LocalAppData%\NuGet\nuget.%NUGET_VERSION%.exe
SET BUILDCMD_KOREBUILD_VERSION=
SET BUILDCMD_DNX_VERSION=

IF EXIST %CACHED_NUGET% goto copynuget
echo Downloading latest version of NuGet.exe...
IF NOT EXIST %LocalAppData%\NuGet md %LocalAppData%\NuGet
@powershell -NoProfile -ExecutionPolicy unrestricted -Command "$ProgressPreference = 'SilentlyContinue'; Invoke-WebRequest 'https://dist.nuget.org/win-x86-commandline/%NUGET_VERSION%/nuget.exe' -OutFile '%CACHED_NUGET%'"

:copynuget
IF EXIST .nuget\nuget.exe goto getsake
md .nuget
copy %CACHED_NUGET% .nuget\nuget.exe > nul

:getsake
IF EXIST packages\Sake goto skipgetsake
.nuget\NuGet.exe install Sake -ExcludeVersion -Source https://www.nuget.org/api/v2/ -Out packages
:skipgetsake

:getkorebuild
IF EXIST packages\KoreBuild-dotnet goto skipgetkorebuild
IF "%BUILDCMD_KOREBUILD_VERSION%"=="" (
    .nuget\nuget.exe install KoreBuild-dotnet -ExcludeVersion -o packages -nocache -pre
) ELSE (
    .nuget\nuget.exe install KoreBuild-dotnet -version %BUILDCMD_KOREBUILD_VERSION% -ExcludeVersion -o packages -nocache -pre
)
:skipgetkorebuild

:getdotnet
SET DOTNET_INSTALL_DIR=packages
CALL packages\KoreBuild-dotnet\build\install.cmd
SET PATH=%DOTNET_INSTALL_DIR%\cli\bin;%PATH%

packages\Sake\tools\Sake.exe -I packages\KoreBuild-dotnet\build -f makefile.shade %*
