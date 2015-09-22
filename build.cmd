@echo off
cd %~dp0

SETLOCAL
SET CACHED_NUGET=%LocalAppData%\NuGet\NuGet.exe
SET BUILDCMD_KOREBUILD_VERSION=""
SET BUILDCMD_DNX_VERSION=""

IF EXIST %CACHED_NUGET% goto copynuget
echo Downloading latest version of NuGet.exe...
IF NOT EXIST %LocalAppData%\NuGet md %LocalAppData%\NuGet
@powershell -NoProfile -ExecutionPolicy unrestricted -Command "$ProgressPreference = 'SilentlyContinue'; Invoke-WebRequest 'https://www.nuget.org/nuget.exe' -OutFile '%CACHED_NUGET%'"

:copynuget
IF EXIST .nuget\nuget.exe goto restore
md .nuget
copy %CACHED_NUGET% .nuget\nuget.exe > nul

:restore
IF EXIST packages\KoreBuild goto run
IF %BUILDCMD_KOREBUILD_VERSION%=="" (
	.nuget\NuGet.exe install KoreBuild -ExcludeVersion -o packages -nocache -pre
) ELSE (
	.nuget\NuGet.exe install KoreBuild -version %BUILDCMD_KOREBUILD_VERSION% -ExcludeVersion -o packages -nocache -pre
)
.nuget\NuGet.exe install Sake -ExcludeVersion -Source https://www.nuget.org/api/v2/ -Out packages

IF "%SKIP_DNX_INSTALL%"=="1" goto run
IF %BUILDCMD_DNX_VERSION%=="" (
	CALL packages\KoreBuild\build\dnvm upgrade -runtime CLR -arch x86
) ELSE (
	CALL packages\KoreBuild\build\dnvm install %BUILDCMD_DNX_VERSION% -runtime CLR -arch x86 -a default
)
CALL packages\KoreBuild\build\dnvm install default -runtime CoreCLR -arch x86

:run
CALL packages\KoreBuild\build\dnvm use default -runtime CLR -arch x86
packages\Sake\tools\Sake.exe -I packages\KoreBuild\build -f makefile.shade %*