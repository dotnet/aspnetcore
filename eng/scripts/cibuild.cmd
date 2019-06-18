@ECHO OFF
SET RepoRoot=%~dp0..\..
%RepoRoot%\build.cmd -ci -all -pack -sign %*
SET exit_code=%ERRORLEVEL%
ECHO build.cmd completed
EXIT /b %exit_code%
