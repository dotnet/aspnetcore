@ECHO OFF
SET RepoRoot=%~dp0..\..\
%RepoRoot%build.cmd -buildJava -projects %~dp0**\*.*proj %*
