@ECHO OFF
SET RepoRoot=%~dp0..\..
%RepoRoot%\build.cmd -projects %~dp0**\*.*proj -msbuildarguments "/p:EnforceE2ETestPrerequisites=true" %*
