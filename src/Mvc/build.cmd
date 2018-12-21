@ECHO OFF
SET RepoRoot="%~dp0..\.."
%RepoRoot%\build.cmd -All -RepoRoot %~dp0 %*