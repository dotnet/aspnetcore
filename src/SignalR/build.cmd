@ECHO OFF
SET RepoRoot=%~dp0..\..
%RepoRoot%\eng\build.cmd -buildJava -projects %~dp0**\*.*proj %*
