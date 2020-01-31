@ECHO OFF
SETLOCAL

if not [%1] == [] (set remote_repo=%1) else (set remote_repo=%RUNTIME_REPO%)

IF [%remote_repo%] == [] (
  echo The 'RUNTIME_REPO' environment variable or command line parameter is not set, aborting.
  exit /b 1
)

echo RUNTIME_REPO: %remote_repo%

robocopy . %remote_repo%\src\libraries\Common\src\System\Net\Http\Http3 /MIR
robocopy .\..\test\Shared.Tests\Http3 %remote_repo%\src\libraries\Common\tests\Tests\System\Net\Http3 /MIR
