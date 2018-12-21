@ECHO OFF
SET RepoRoot=%~dp0..\..
%RepoRoot%\build.cmd -LockFile %RepoRoot%\korebuild-lock.txt -projects %~dp0**\*.*proj %*
