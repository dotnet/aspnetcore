@ECHO off
SETLOCAL

IF "%KOREBUILD_FOLDER%"=="" (
	ECHO Error: KOREBUILD_FOLDER is not set.
	EXIT /B 1
)
IF "%NUGET_PATH%"=="" (
	ECHO Error: NUGET_PATH is not set.
	EXIT /B 1
)

IF NOT EXIST Sake  (
    "%NUGET_PATH%" install Sake -ExcludeVersion -Source https://api.nuget.org/v3/index.json -o %~dp0
)

IF NOT EXIST xunit.runner.console  (
    "%NUGET_PATH%" install xunit.runner.console -ExcludeVersion -Source https://api.nuget.org/v3/index.json -o %~dp0
)

SET DOTNET_LOCAL_INSTALL_FOLDER=%LOCALAPPDATA%\Microsoft\dotnet\cli\bin

CALL %~dp0dotnet-install.cmd
ECHO Adding %DOTNET_LOCAL_INSTALL_FOLDER% to PATH
SET PATH=%DOTNET_LOCAL_INSTALL_FOLDER%;%PATH%
SET DOTNET_HOME=%DOTNET_LOCAL_INSTALL_FOLDER%

REM ==== Temporary ====		
IF "%BUILDCMD_DNX_VERSION%"=="" (		
    SET BUILDCMD_DNX_VERSION=latest		
)		
IF "%SKIP_DNX_INSTALL%"=="" (		
    CALL %KOREBUILD_FOLDER%\build\dnvm install %BUILDCMD_DNX_VERSION% -runtime CoreCLR -arch x86 -alias default		
    CALL %KOREBUILD_FOLDER%\build\dnvm install default -runtime CLR -arch x86 -alias default		
) ELSE (		
    CALL %KOREBUILD_FOLDER%\build\dnvm use default -runtime CLR -arch x86		
)		
REM ============================
 
SET MAKEFILE_PATH=makefile.shade
IF NOT EXIST %MAKEFILE_PATH% (
	SET MAKEFILE_PATH=%KOREBUILD_FOLDER%\build\makefile.shade
)
ECHO Using makefile: %MAKEFILE_PATH%


REM Don't use full paths. Sake doesn't support them!
"%~dp0Sake\tools\Sake.exe" -I %KOREBUILD_FOLDER%\build -f %MAKEFILE_PATH% %*