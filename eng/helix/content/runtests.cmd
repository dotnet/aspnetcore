@echo off
SETLOCAL

REM Use '$' as a variable name prefix to avoid MSBuild variable collisions with these variables
set $target=%1
set $sdkVersion=%2
set $runtimeVersion=%3
set $aspRuntimeVersion=%4
set $queue=%5
set $arch=%6
set $quarantined=%7
set $ef=%8
set $aspnetruntime=%9
REM Batch only supports up to 9 arguments using the %# syntax, need to shift to get more
shift
set $aspnetref=%9
shift
set $helixTimeout=%9
shift
set $feedCred=%9

set DOTNET_HOME=%HELIX_CORRELATION_PAYLOAD%\sdk
set DOTNET_ROOT=%DOTNET_HOME%\%$arch%
set DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
set DOTNET_MULTILEVEL_LOOKUP=0
set DOTNET_CLI_HOME=%HELIX_CORRELATION_PAYLOAD%\home
set VSTEST_DUMP_PATH=%HELIX_WORKITEM_ROOT%

set "PATH=%DOTNET_ROOT%;%PATH%;%HELIX_CORRELATION_PAYLOAD%\node\bin"
echo Set path to: "%PATH%"

powershell.exe -noLogo -NoProfile -ExecutionPolicy unrestricted -command ". eng\common\tools.ps1; InstallDotNet %DOTNET_ROOT% %$sdkVersion% %$arch% '' $true '' '' $true"
IF [%$feedCred%] == [] (
    powershell.exe -noLogo -NoProfile -ExecutionPolicy unrestricted -command ". eng\common\tools.ps1; InstallDotNet %DOTNET_ROOT% %$runtimeVersion% %$arch% dotnet $true '' '' $true"
) else (
    powershell.exe -noLogo -NoProfile -ExecutionPolicy unrestricted -command ". eng\common\tools.ps1; InstallDotNet %DOTNET_ROOT% %$runtimeVersion% %$arch% dotnet $true https://dotnetclimsrc.blob.core.windows.net/dotnet %$feedCred% $true"
)

set exit_code=0

echo "Restore: dotnet restore RunTests\RunTests.csproj --ignore-failed-sources"
dotnet restore RunTests\RunTests.csproj --ignore-failed-sources

echo "Running tests: dotnet run --no-restore --project RunTests\RunTests.csproj -- --target %$target% --runtime %$aspRuntimeVersion% --queue %$queue% --arch %$arch% --quarantined %$quarantined% --ef %$ef% --aspnetruntime %$aspnetruntime% --aspnetref %$aspnetref% --helixTimeout %$helixTimeout%"
dotnet run --no-restore --project RunTests\RunTests.csproj -- --target %$target% --runtime %$aspRuntimeVersion% --queue %$queue% --arch %$arch% --quarantined %$quarantined% --ef %$ef% --aspnetruntime %$aspnetruntime% --aspnetref %$aspnetref% --helixTimeout %$helixTimeout%
if errorlevel neq 0 (
    set exit_code=%errorlevel%
)
echo "Finished running tests: exit_code=%exit_code%"
exit /b %exit_code%
