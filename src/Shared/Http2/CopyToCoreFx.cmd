@ECHO OFF
SETLOCAL

if not [%1] == [] (set remote_repo=%1) else (set remote_repo=%COREFX_REPO%)

IF [%remote_repo%] == [] (
  echo The 'COREFX_REPO' environment variable or command line paramter is not set, aborting.
  exit /b 1
)

echo COREFX_REPO: %remote_repo%

robocopy . %remote_repo%\src\Common\src\System\Net\Http\Http2 /MIR
robocopy .\..\test\Shared.Tests\Http2 %remote_repo%\src\Common\tests\Tests\System\Net\Http2 /MIR