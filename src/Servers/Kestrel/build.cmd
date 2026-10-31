@ECHO OFF
SET RepoRoot=%~dp0..\..\..
%RepoRoot%\eng\build.cmd -nobuildnative -projects %~dp0**\*.*proj %*
