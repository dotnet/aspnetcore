@echo off
cd %~dp0

IF EXIST .nuget\NuGet.exe goto part2
echo Downloading latest version of NuGet.exe...
md .nuget
@powershell -NoProfile -ExecutionPolicy unrestricted -Command "$ProgressPreference = 'SilentlyContinue'; Invoke-WebRequest 'https://www.nuget.org/nuget.exe' -OutFile '.nuget\NuGet.exe'"

:part2
.nuget\NuGet.exe install Sake -version 0.2 -o packages
packages\Sake.0.2\tools\Sake.exe -I build -f makefile.shade %*
