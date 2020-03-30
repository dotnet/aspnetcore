@echo off
REM Need delayed expansion !PATH! so parens in the path don't mess up the parens for the if statements that use parens for blocks
setlocal enabledelayedexpansion

REM Use '$' as a variable name prefix to avoid MSBuild variable collisions with these variables
set $target=%1
set $sdkVersion=%2
set $runtimeVersion=%3
set $helixQueue=%4
set $arch=%5
set $quarantined=%6
set $efVersion=%7

set DOTNET_HOME=%HELIX_CORRELATION_PAYLOAD%\sdk
set DOTNET_ROOT=%DOTNET_HOME%\%$arch%
set DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
set DOTNET_MULTILEVEL_LOOKUP=0
set DOTNET_CLI_HOME=%HELIX_CORRELATION_PAYLOAD%\home

set PATH=%DOTNET_ROOT%;!PATH!;%HELIX_CORRELATION_PAYLOAD%\node\bin
echo Set path to: %PATH%
echo "Installing SDK"
powershell.exe -NoProfile -ExecutionPolicy unrestricted -Command "[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; &([scriptblock]::Create((Invoke-WebRequest -useb 'https://dot.net/v1/dotnet-install.ps1'))) -Architecture %$arch% -Version %$sdkVersion% -InstallDir %DOTNET_ROOT%"
echo "Installing Runtime"
powershell.exe -NoProfile -ExecutionPolicy unrestricted -Command "[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; &([scriptblock]::Create((Invoke-WebRequest -useb 'https://dot.net/v1/dotnet-install.ps1'))) -Architecture %$arch% -Runtime dotnet -Version %$runtimeVersion% -InstallDir %DOTNET_ROOT%"

set ASPNETCORE_TEST_TARGET=%$target%
set ASPNETCORE_SDK_VERSION=%$sdkVersion%
set ASPNETCORE_RUNTIME_VERSION=%$runtimeVersion%
set ASPNETCORE_HELIX_QUEUE=%$helixQueue%
set ASPNETCORE_ARCHITECTURE=%$arch%
set ASPNETCORE_QUARANTINED=%$quarantined%
set ASPNETCORE_EF_VERSION=%$efVersion%

set exit_code=0
dotnet run --project app\app.csproj
if errorlevel 1 (
    set exit_code=1
)

exit /b %exit_code%
