@ECHO off
SETLOCAL

SET REPO_FOLDER=%~dp0
CD %REPO_FOLDER%

SET BUILD_FOLDER=.build
SET KOREBUILD_FOLDER=%BUILD_FOLDER%\KoreBuild-dotnet
SET KOREBUILD_VERSION=

SET NUGET_PATH=%BUILD_FOLDER%\NuGet.exe
SET NUGET_VERSION=latest
SET CACHED_NUGET=%LocalAppData%\NuGet\nuget.%NUGET_VERSION%.exe

IF NOT EXIST %BUILD_FOLDER% (
    md %BUILD_FOLDER%
)

IF NOT EXIST %NUGET_PATH% (
    IF NOT EXIST %CACHED_NUGET% (
        echo Downloading latest version of NuGet.exe...
        IF NOT EXIST %LocalAppData%\NuGet ( 
            md %LocalAppData%\NuGet
        )
        @powershell -NoProfile -ExecutionPolicy unrestricted -Command "$ProgressPreference = 'SilentlyContinue'; Invoke-WebRequest 'https://dist.nuget.org/win-x86-commandline/%NUGET_VERSION%/nuget.exe' -OutFile '%CACHED_NUGET%'"
    )

    copy %CACHED_NUGET% %NUGET_PATH% > nul
)

IF NOT EXIST %KOREBUILD_FOLDER% (
    SET KOREBUILD_DOWNLOAD_ARGS=
    IF NOT "%KOREBUILD_VERSION%"=="" (
        SET KOREBUILD_DOWNLOAD_ARGS=-version %KOREBUILD_VERSION%
    )
    
    %BUILD_FOLDER%\nuget.exe install KoreBuild-dotnet -ExcludeVersion -o %BUILD_FOLDER% -nocache -pre %KOREBUILD_DOWNLOAD_ARGS%
)

"%KOREBUILD_FOLDER%\build\KoreBuild.cmd" %*
