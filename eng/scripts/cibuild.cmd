@ECHO OFF
SET RepoRoot=%~dp0..\..
%RepoRoot%\build.cmd -prepareMachine -ci -all -pack -sign %*
