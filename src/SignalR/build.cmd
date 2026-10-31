@ECHO OFF
SET RepoRoot=%~dp0..\..
%RepoRoot%\eng\build.cmd -nobuildnative -buildJava -projects %~dp0**\*.*proj %*
