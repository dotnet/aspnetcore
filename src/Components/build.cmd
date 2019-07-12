@ECHO OFF
SET RepoRoot=%~dp0..\..

REM Building Web.JS first explicitly to workaround ordering issues between csproj and npmproj.
ECHO Building Web.JS
CALL %RepoRoot%\build.cmd -projects %~dp0Web.JS\Microsoft.AspNetCore.Components.Web.JS.npmproj "/p:EnforceE2ETestPrerequisites=true" %*

IF %ERRORLEVEL% NEQ 0 (
   EXIT /b %ErrorLevel%
)

ECHO Building Components
CALL %RepoRoot%\build.cmd -projects %~dp0**\*.*proj "/p:EnforceE2ETestPrerequisites=true" %*

IF %ERRORLEVEL% NEQ 0 (
   EXIT /b %ErrorLevel%
)

ECHO Components successfully built!