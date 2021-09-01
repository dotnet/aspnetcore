@ECHO OFF
SET RepoRoot=%~dp0..\..
CALL "%RepoRoot%\eng\build.cmd" -projects "%~dp0*\*.*proj" "/p:EnforceE2ETestPrerequisites=true" %*
IF %ERRORLEVEL% NEQ 0 (
   EXIT /b %ErrorLevel%
)

CALL "%RepoRoot%\eng\build.cmd" -projects "%RepoRoot%\src\submodules\spa-templates\src\*.*proj" "/p:EnforceE2ETestPrerequisites=true" -noBuildRepoTasks -noBuildNative %*
IF %ERRORLEVEL% NEQ 0 (
   EXIT /b %ErrorLevel%
)
