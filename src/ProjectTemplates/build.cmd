@ECHO OFF
SET RepoRoot=%~dp0..\..
CALL "%RepoRoot%\eng\build.cmd" -projects "%~dp0*\*.*proj" "/p:EnforceE2ETestPrerequisites=true" %*
IF %ERRORLEVEL% NEQ 0 (
   EXIT /b %ErrorLevel%
)
