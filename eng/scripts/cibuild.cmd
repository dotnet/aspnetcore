@ECHO OFF
SET RepoRoot=%~dp0..\..
%RepoRoot%\build.cmd -ci -all -restore -build -pack -test -sign %*
