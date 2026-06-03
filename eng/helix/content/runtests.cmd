@ECHO OFF
SETLOCAL EnableDelayedExpansion

REM Use '$' as a variable name prefix to avoid MSBuild variable collisions with these variables
REM First arg is always @targets.txt
set "$firstArg=%~1"
set "$targetsFile=!$firstArg:~1!"
set "$aspRuntimeVersion=%~2"
set "$queue=%~3"
set "$arch=%~4"
set "$quarantined=%~5"
set "$helixTimeout=%~6"
set "$installPlaywright=%~7"

set DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
set DOTNET_NOLOGO=1
set DOTNET_GENERATE_ASPNET_CERTIFICATE=false
set PLAYWRIGHT_BROWSERS_PATH=%CD%\ms-playwright

REM Avoid https://github.com/dotnet/aspnetcore/issues/41937 in current session.
set ASPNETCORE_ENVIRONMENT=

set "PATH=%HELIX_WORKITEM_ROOT%;%PATH%;%HELIX_WORKITEM_ROOT%\node\bin"
echo Set path to: "%PATH%"
echo.

echo "Running tests: dotnet %HELIX_CORRELATION_PAYLOAD%/HelixTestRunner/HelixTestRunner.dll --targets-file !$targetsFile! --runtime !$aspRuntimeVersion! --queue !$queue! --arch !$arch! --quarantined !$quarantined! --helixTimeout !$helixTimeout! --playwright !$installPlaywright!"
dotnet %HELIX_CORRELATION_PAYLOAD%/HelixTestRunner/HelixTestRunner.dll --targets-file !$targetsFile! --runtime !$aspRuntimeVersion! --queue !$queue! --arch !$arch! --quarantined !$quarantined! --helixTimeout !$helixTimeout! --playwright !$installPlaywright!
set exit_code=%errorlevel%

echo "Finished running tests: exit_code=%exit_code%"
EXIT /b %exit_code%
