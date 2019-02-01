@ECHO OFF
SET RepoRoot="%~dp0..\..\.."
%RepoRoot%\build.cmd -Pack -ForceCoreMsBuild -Projects "%~dp0\**\*.csproj" %*
