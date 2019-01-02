@ECHO OFF
SET RepoRoot=%~dp0..\..
%RepoRoot%\build.cmd -LockFile %RepoRoot%\korebuild-lock.txt -Projects %~dp0\**\*.*proj %*
