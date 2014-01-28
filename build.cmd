@echo off
cd %~dp0

IF EXIST .nuget\NuGet.exe goto restore
echo Downloading latest version of NuGet.exe...
md .nuget
@powershell -NoProfile -ExecutionPolicy unrestricted -Command "$ProgressPreference = 'SilentlyContinue'; Invoke-WebRequest 'https://www.nuget.org/nuget.exe' -OutFile '.nuget\NuGet.exe'"

:restore
IF EXIST build goto run
.nuget\NuGet.exe install KoreBuild -ExcludeVersion -o packages -nocache -pre
xcopy packages\KoreBuild\build build\ /Y
.nuget\NuGet.exe install Sake -version 0.2 -o packages

:run
packages\Sake.0.2\tools\Sake.exe -I build -f makefile.shade %*
