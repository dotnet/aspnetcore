@ECHO OFF
SET RepoRoot=%~dp0..\..

ECHO Building Microsoft.AspNetCore.Runtime.SiteExtension
CALL "%RepoRoot%\build.cmd" -projects "%~dp0Runtime\Microsoft.AspNetCore.Runtime.SiteExtension.pkgproj" %*
IF %ERRORLEVEL% NEQ 0 (
   EXIT /b %ErrorLevel%
)

ECHO Building LoggingBranch
REM /p:DisableTransitiveFrameworkReferences=true is needed to prevent SDK from picking up transitive references to
REM Microsoft.AspNetCore.App as framework references https://github.com/dotnet/sdk/pull/3221
CALL "%RepoRoot%\build.cmd" -projects "%~dp0LoggingBranch\LB.csproj" ^
    /p:DisableTransitiveFrameworkReferences=true %*
IF %ERRORLEVEL% NEQ 0 (
   EXIT /b %ErrorLevel%
)

ECHO SiteExtensions successfully built!