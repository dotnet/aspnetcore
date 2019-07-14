@ECHO OFF
SET RepoRoot=%~dp0..\..\..
%RepoRoot%\build.cmd -BuildNative -projects %~dp0**\*.csproj %* 
