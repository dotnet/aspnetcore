@echo off
REM Need delayed expansion !PATH! so parens in the path don't mess up the parens for the if statements that use parens for blocks
setlocal enabledelayedexpansion

REM Use '$' as a variable name prefix to avoid MSBuild variable collisions with these variables
set $target=%1
set $sdkVersion=%2
set $runtimeVersion=%3
set $queue=%4
set $arch=%5
set $quarantined=%6
set $ef=%7
set $aspnetruntime=%8
set $aspnetref=%9
REM Batch only supports up to 9 arguments using the %# syntax, need to shift to get more
shift
set $helixTimeout=%9

set DOTNET_HOME=%HELIX_CORRELATION_PAYLOAD%\sdk
set DOTNET_ROOT=%DOTNET_HOME%\%$arch%
set DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
set DOTNET_MULTILEVEL_LOOKUP=0
set DOTNET_CLI_HOME=%HELIX_CORRELATION_PAYLOAD%\home

set PATH=%DOTNET_ROOT%;!PATH!;%HELIX_CORRELATION_PAYLOAD%\node\bin
echo Set path to: %PATH%
echo "Invoking InstallDotNet.ps1 %$arch% %$sdkVersion% %$runtimeVersion% %DOTNET_ROOT%"
powershell.exe -NoProfile -ExecutionPolicy unrestricted -file InstallDotNet.ps1 %$arch% %$sdkVersion% %$runtimeVersion% %DOTNET_ROOT%

set exit_code=0
echo "Restore: dotnet restore RunTests\RunTests.csproj --source https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet5/nuget/v3/index.json --source https://api.nuget.org/v3/index.json --ignore-failed-sources..."
dotnet restore RunTests\RunTests.csproj --source https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet5/nuget/v3/index.json --source https://api.nuget.org/v3/index.json --ignore-failed-sources

echo "Running tests: dotnet run --project RunTests\RunTests.csproj -- --target %$target% --sdk %$sdkVersion% --runtime %$runtimeVersion% --queue %$queue% --arch %$arch% --quarantined %$quarantined% --ef %$ef% --aspnetruntime %$aspnetruntime% --aspnetref %$aspnetref% --helixTimeout %$helixTimeout%..."
dotnet run --project RunTests\RunTests.csproj -- --target %$target% --sdk %$sdkVersion% --runtime %$runtimeVersion% --queue %$queue% --arch %$arch% --quarantined %$quarantined% --ef %$ef% --aspnetruntime %$aspnetruntime% --aspnetref %$aspnetref% --helixTimeout %$helixTimeout%
if errorlevel neq 0 (
    set exit_code=%errorlevel%
)
echo "Finished running tests: exit_code=%exit_code%"
exit /b %exit_code%
