@ECHO OFF
SETLOCAL

IF "%COREFX_REPO%" == "" (
  echo The 'COREFX_REPO' environment variable is not set, aborting.
  exit /b 1
)

echo COREFX_REPO: %COREFX_REPO%

robocopy . %COREFX_REPO%\src\Common\src\System\Net\Http\Http2 /MIR
robocopy .\..\test\Shared.Tests\Http2 %COREFX_REPO%\src\Common\tests\Tests\System\Net\Http2