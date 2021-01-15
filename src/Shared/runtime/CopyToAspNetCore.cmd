@ECHO OFF
SETLOCAL

if not [%1] == [] (set remote_repo=%1) else (set remote_repo=%ASPNETCORE_REPO%)

IF [%remote_repo%] == [] (
  echo The 'ASPNETCORE_REPO' environment variable or command line parameter is not set, aborting.
  exit /b 1
)

echo ASPNETCORE_REPO: %remote_repo%

REM https://superuser.com/questions/280425/getting-robocopy-to-return-a-proper-exit-code
(robocopy . %remote_repo%\src\Shared\runtime /MIR) ^& IF %ERRORLEVEL% LSS 8 SET ERRORLEVEL = 0
(robocopy .\..\..\..\..\..\tests\Tests\System\Net\aspnetcore\ %remote_repo%\src\Shared\test\Shared.Tests\runtime /MIR) ^& IF %ERRORLEVEL% LSS 8 SET ERRORLEVEL = 0
