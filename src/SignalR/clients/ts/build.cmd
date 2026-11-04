@ECHO OFF
SET RepoRoot=%~dp0..\..\..\..
%RepoRoot%\eng\build.cmd -projects %~dp0**\*.*proj %*
