@echo off
cd %~dp0

IF EXIST .nuget\NuGet.exe goto part2
echo Downloading latest version of NuGet.exe...
mkdir .nuget
@powershell -NoProfile -ExecutionPolicy unrestricted -Command "((new-object net.webclient).DownloadFile('https://nuget.org/nuget.exe', '.nuget\NuGet.exe'))"

:part2
set EnableNuGetPackageRestore=true
.nuget\NuGet.exe install Sake -version 0.2 -o packages
packages\Sake.0.2\tools\Sake.exe -I build -f makefile.shade %*
