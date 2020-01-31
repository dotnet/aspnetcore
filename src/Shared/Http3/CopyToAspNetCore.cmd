@ECHO OFF
SETLOCAL

if not [%1] == [] (set remote_repo=%1) else (set remote_repo=%ASPNETCORE_REPO%)

IF [%remote_repo%] == [] (
  echo The 'ASPNETCORE_REPO' environment variable or command line parameter is not set, aborting.
  exit /b 1
)

echo ASPNETCORE_REPO: %remote_repo%

robocopy . %remote_repo%\src\Shared\Http3 /MIR
robocopy .\..\..\..\..\..\tests\Tests\System\Net\Http3\ %remote_repo%\src\Shared\test\Shared.Tests\Http3 /MIR
