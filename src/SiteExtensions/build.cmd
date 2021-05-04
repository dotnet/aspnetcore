@ECHO OFF
SET RepoRoot=%~dp0..\..

ECHO Building x64 Microsoft.AspNetCore.Runtime.SiteExtension
CALL "%RepoRoot%\eng\build.cmd" -arch x64 -projects "%~dp0Runtime\Microsoft.AspNetCore.Runtime.SiteExtension.pkgproj" ^
    "/bl:%RepoRoot%/artifacts/log/SiteExtensions-Runtime-x64.binlog" %*
IF %ERRORLEVEL% NEQ 0 (
   EXIT /b %ErrorLevel%
)

ECHO Building x86 Microsoft.AspNetCore.Runtime.SiteExtension
CALL "%RepoRoot%\eng\build.cmd" -arch x86 -projects "%~dp0Runtime\Microsoft.AspNetCore.Runtime.SiteExtension.pkgproj" ^
    "/bl:%RepoRoot%/artifacts/log/SiteExtensions-Runtime-x86.binlog" %*
IF %ERRORLEVEL% NEQ 0 (
   EXIT /b %ErrorLevel%
)

ECHO Building x64 LoggingBranch
REM /p:DisableTransitiveFrameworkReferences=true is needed to prevent SDK from picking up transitive references to
REM Microsoft.AspNetCore.App as framework references https://github.com/dotnet/sdk/pull/3221
CALL "%RepoRoot%\eng\build.cmd" -arch x64 -projects "%~dp0LoggingBranch\LB.csproj" ^
    /p:DisableTransitiveFrameworkReferences=true "/bl:%RepoRoot%/artifacts/log/SiteExtensions-LoggingBranch-x64.binlog" %*
IF %ERRORLEVEL% NEQ 0 (
   EXIT /b %ErrorLevel%
)

ECHO Building x86 LoggingBranch
CALL "%RepoRoot%\eng\build.cmd" -arch x86 -projects "%~dp0LoggingBranch\LB.csproj" ^
    /p:DisableTransitiveFrameworkReferences=true "/bl:%RepoRoot%/artifacts/log/SiteExtensions-LoggingBranch-x86.binlog" %*
IF %ERRORLEVEL% NEQ 0 (
   EXIT /b %ErrorLevel%
)

ECHO Building Microsoft.AspNetCore.AzureAppServices.SiteExtension
CALL "%RepoRoot%\eng\build.cmd" -projects ^
    "%~dp0LoggingAggregate\src\Microsoft.AspNetCore.AzureAppServices.SiteExtension\Microsoft.AspNetCore.AzureAppServices.SiteExtension.csproj" ^
    "/bl:%RepoRoot%/artifacts/log/SiteExtensions-LoggingAggregate.binlog" %*
IF %ERRORLEVEL% NEQ 0 (
   EXIT /b %ErrorLevel%
)

ECHO SiteExtensions successfully built!
