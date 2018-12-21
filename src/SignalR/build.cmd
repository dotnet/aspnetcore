@ECHO OFF
SET RepoRoot="%~dp0..\.."
%RepoRoot%\build.cmd -RepoRoot %~dp0 %*
