@ECHO OFF
SET RepoRoot=%~dp0..\..

ECHO Building Microsoft.AspNetCore.Runtime.SiteExtension
CALL "%RepoRoot%\build.cmd" -arch x64 -projects "%~dp0Runtime\Microsoft.AspNetCore.Runtime.SiteExtension.pkgproj" ^
    "/bl:%RepoRoot%/artifacts/log/SiteExtensions-Runtime-x86.binlog" %*
IF %ERRORLEVEL% NEQ 0 (
   EXIT /b %ErrorLevel%
)

ECHO Building LoggingBranch
REM /p:DisableTransitiveFrameworkReferences=true is needed to prevent SDK from picking up transitive references to
REM Microsoft.AspNetCore.App as framework references https://github.com/dotnet/sdk/pull/3221
CALL "%RepoRoot%\build.cmd" -arch x64 -projects "%~dp0LoggingBranch\LB.csproj" ^
    /p:DisableTransitiveFrameworkReferences=true "/bl:%RepoRoot%/artifacts/log/SiteExtensions-LoggingBranch-x64.binlog" %*
IF %ERRORLEVEL% NEQ 0 (
   EXIT /b %ErrorLevel%
)

ECHO SiteExtensions successfully built!
