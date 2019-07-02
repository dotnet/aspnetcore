@ECHO OFF
SET BUILD_IIS_NATIVE_PROJECTS=true

%~dp0..\..\..\startvs.cmd %~dp0IISIntegration.sln
