@ECHO OFF

:: This command launches a Visual Studio solution with environment variables required to use a local version of the .NET Core SDK.

IF "%DOTNET_HOME%"=="" (
    set DOTNET_HOME=%USERPROFILE%\.dotnet\x64
)

:: This tells .NET Core to use the same dotnet.exe that build scripts use
SET DOTNET_ROOT=%DOTNET_HOME%

:: This tells .NET Core not to go looking for .NET Core in other places
SET DOTNET_MULTILEVEL_LOOKUP=0

:: Put our local dotnet.exe on PATH first so Visual Studio knows which one to use
SET PATH=%DOTNET_ROOT%;%PATH%

SET sln=%1

IF NOT EXIST "%DOTNET_ROOT%\dotnet.exe" (
    echo .NET Core has not yet been installed. Run `build.cmd -restore` to install tools
    exit /b 1
)

IF "%sln%"=="" (
    echo Error^: Expected argument ^<SLN_FILE^>
    echo Usage^: startvs.cmd ^<SLN_FILE^>

    exit /b 1
)

start %sln%
