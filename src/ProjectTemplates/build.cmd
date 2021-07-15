@ECHO OFF
SET RepoRoot=%~dp0..\..
%RepoRoot%\eng\build.cmd -projects %~dp0*\*.*proj "/p:EnforceE2ETestPrerequisites=true" %*
%RepoRoot%\eng\build.cmd -projects %~dp0\..\submodules\spa-templates\src\*.*proj "/p:EnforceE2ETestPrerequisites=true" %*
