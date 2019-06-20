@ECHO OFF
SET RepoRoot=%~dp0..\..

ECHO Building Microsoft.AspNetCore.Runtime.SiteExtension
CALL %RepoRoot%\build.cmd -projects %~dp0Runtime\Microsoft.AspNetCore.Runtime.SiteExtension.pkgproj /bl:artifacts/log/se-runtime.binlog %*

IF %ERRORLEVEL% NEQ 0 (
   EXIT /b %ErrorLevel%
)

ECHO Building LoggingBranch
CALL %RepoRoot%\build.cmd -projects %~dp0LoggingBranch\LB.csproj /bl:artifacts/log/se-lb.binlog %*

IF %ERRORLEVEL% NEQ 0 (
   EXIT /b %ErrorLevel%
)

ECHO Building Microsoft.AspNetCore.AzureAppServices.SiteExtension
CALL %RepoRoot%\build.cmd -projects %~dp0LoggingAggregate\src\Microsoft.AspNetCore.AzureAppServices.SiteExtension\Microsoft.AspNetCore.AzureAppServices.SiteExtension.csproj /bl:artifacts/log/se-aas.binlog %*

IF %ERRORLEVEL% NEQ 0 (
   EXIT /b %ErrorLevel%
)

ECHO SiteExtensions successly built!