@ECHO OFF
SET RepoRoot=%~dp0..\..

ECHO Building Microsoft.AspNetCore.Runtime.SiteExtension
CALL %RepoRoot%\build.cmd -arch x64 -projects %~dp0Runtime\Microsoft.AspNetCore.Runtime.SiteExtension.pkgproj /bl:artifacts/log/SiteExtensions-Runtime-x64.binlog %*
CALL %RepoRoot%\build.cmd -arch x86 -projects %~dp0Runtime\Microsoft.AspNetCore.Runtime.SiteExtension.pkgproj /bl:artifacts/log/SiteExtensions-Runtime-x86.binlog %*

IF %ERRORLEVEL% NEQ 0 (
   EXIT /b %ErrorLevel%
)

ECHO Building LoggingBranch
REM Remove /p:DisablePackageAssetsCache=true /p:DisableTransitiveFrameworkReferences=true which workarounds dotnet store and publish behaviour changes in preview 6
CALL %RepoRoot%\build.cmd -forceCoreMsbuild -arch x64 -projects %~dp0LoggingBranch\LB.csproj /p:DisablePackageAssetsCache=true /p:DisableTransitiveFrameworkReferences=true /bl:artifacts/log/SiteExtensions-LoggingBranch-x64.binlog %*
CALL %RepoRoot%\build.cmd -forceCoreMsbuild -arch x86 -projects %~dp0LoggingBranch\LB.csproj /p:DisablePackageAssetsCache=true /p:DisableTransitiveFrameworkReferences=true /bl:artifacts/log/SiteExtensions-LoggingBranch-x86.binlog %*

IF %ERRORLEVEL% NEQ 0 (
   EXIT /b %ErrorLevel%
)

ECHO Building Microsoft.AspNetCore.AzureAppServices.SiteExtension
CALL %RepoRoot%\build.cmd -forceCoreMsbuild -projects %~dp0LoggingAggregate\src\Microsoft.AspNetCore.AzureAppServices.SiteExtension\Microsoft.AspNetCore.AzureAppServices.SiteExtension.csproj /bl:artifacts/log/SiteExtensions-LoggingAggregate.binlog %*

IF %ERRORLEVEL% NEQ 0 (
   EXIT /b %ErrorLevel%
)

ECHO SiteExtensions successfully built!