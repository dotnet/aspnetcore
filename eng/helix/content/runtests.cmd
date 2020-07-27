@echo off
REM Need delayed expansion !PATH! so parens in the path don't mess up the parens for the if statements that use parens for blocks
setlocal enabledelayedexpansion

REM Use '$' as a variable name prefix to avoid MSBuild variable collisions with these variables
set $target=%1
set $runtimeVersion=%2
set $queue=%3
set $arch=%4
set $quarantined=%5
set $ef=%6
set $aspnetruntime=%7
set $aspnetref=%8
set $helixTimeout=%9
REM Batch only supports up to 9 arguments using the %# syntax, need to shift to get more

set DOTNET_ROOT=%HELIX_WORKITEM_ROOT%\dotnet
set DOTNET_HOME=%DOTNET_ROOT%
set DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
set DOTNET_MULTILEVEL_LOOKUP=0
set DOTNET_CLI_HOME=%DOTNET_ROOT%\home

set PATH=%DOTNET_ROOT%;!PATH!;%HELIX_CORRELATION_PAYLOAD%\node\bin
echo Set path to: %PATH%

echo "xcopy /s /i %HELIX_CORRELATION_PAYLOAD%\dotnet %DOTNET_ROOT%"
xcopy /s /i %HELIX_CORRELATION_PAYLOAD%\dotnet %DOTNET_ROOT%

echo "Invoking InstallDotNet.ps1 %$arch% %$runtimeVersion% %DOTNET_ROOT%"
powershell.exe -NoProfile -ExecutionPolicy unrestricted -file InstallDotNet.ps1 %$arch% %$runtimeVersion% %DOTNET_ROOT%

dotnet --list-sdks
dotnet --list-runtimes

set exit_code=0
echo "Restore: dotnet restore RunTests\RunTests.csproj --source https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet5/nuget/v3/index.json --source https://api.nuget.org/v3/index.json --ignore-failed-sources..."
dotnet restore RunTests\RunTests.csproj --source https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet5/nuget/v3/index.json --source https://api.nuget.org/v3/index.json --ignore-failed-sources

echo "Running tests: dotnet run --project RunTests\RunTests.csproj -- --target %$target% --runtime %$runtimeVersion% --queue %$queue% --arch %$arch% --quarantined %$quarantined% --ef %$ef% --aspnetruntime %$aspnetruntime% --aspnetref %$aspnetref% --helixTimeout %$helixTimeout%..."
dotnet run --project RunTests\RunTests.csproj -- --target %$target% --runtime %$runtimeVersion% --queue %$queue% --arch %$arch% --quarantined %$quarantined% --ef %$ef% --aspnetruntime %$aspnetruntime% --aspnetref %$aspnetref% --helixTimeout %$helixTimeout%
if errorlevel neq 0 (
    set exit_code=%errorlevel%
)
echo "Finished running tests: exit_code=%exit_code%"
exit /b %exit_code%
