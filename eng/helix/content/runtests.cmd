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
set $helixTimeout=%9
REM Batch only supports up to 9 arguments using the %# syntax, need to shift to get more
shift
set $feedCred=%9

set DOTNET_HOME=%CD%\sdk%random%
set DOTNET_ROOT=%DOTNET_HOME%\%$arch%
set DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
set DOTNET_MULTILEVEL_LOOKUP=0
set DOTNET_CLI_HOME=%CD%\home%random%

set "PATH=%DOTNET_ROOT%;%PATH%;%CD%\nodejs"
echo Set path to: "%PATH%"
echo.

IF [%$feedCred%] == [] (
    echo "InstallDotNet %DOTNET_ROOT% %$sdkVersion% %$arch% '' $true '' '' $true"
    powershell.exe -noLogo -NoProfile -ExecutionPolicy unrestricted -command ". eng\common\tools.ps1; InstallDotNet %DOTNET_ROOT% %$sdkVersion% %$arch% '' $true '' '' $true"
    echo.

    echo "InstallDotNet %DOTNET_ROOT% %$runtimeVersion% %$arch% dotnet $true '' '' $true"
    powershell.exe -noLogo -NoProfile -ExecutionPolicy unrestricted -command ". eng\common\tools.ps1; InstallDotNet %DOTNET_ROOT% %$runtimeVersion% %$arch% dotnet $true '' '' $true"
) else (
    echo "InstallDotNet %DOTNET_ROOT% %$sdkVersion% %$arch% '' $true https://dotnetclimsrc.blob.core.windows.net/dotnet ... $true"
    powershell.exe -noLogo -NoProfile -ExecutionPolicy unrestricted -command ". eng\common\tools.ps1; InstallDotNet %DOTNET_ROOT% %$sdkVersion% %$arch% '' $true https://dotnetclimsrc.blob.core.windows.net/dotnet %$feedCred% $true"
    echo.

    echo "InstallDotNet %DOTNET_ROOT% %$runtimeVersion% %$arch% dotnet $true https://dotnetclimsrc.blob.core.windows.net/dotnet ... $true"
    powershell.exe -noLogo -NoProfile -ExecutionPolicy unrestricted -command ". eng\common\tools.ps1; InstallDotNet %DOTNET_ROOT% %$runtimeVersion% %$arch% dotnet $true https://dotnetclimsrc.blob.core.windows.net/dotnet %$feedCred% $true"
)
echo.

set exit_code=0

echo "Restore: dotnet restore RunTests\RunTests.csproj --ignore-failed-sources"
dotnet restore RunTests\RunTests.csproj --ignore-failed-sources

echo "Running tests: dotnet run --no-restore --project RunTests\RunTests.csproj -- --target %$target% --runtime %$aspRuntimeVersion% --queue %$queue% --arch %$arch% --quarantined %$quarantined% --ef %$ef% --helixTimeout %$helixTimeout%"
dotnet run --no-restore --project RunTests\RunTests.csproj -- --target %$target% --runtime %$aspRuntimeVersion% --queue %$queue% --arch %$arch% --quarantined %$quarantined% --ef %$ef% --helixTimeout %$helixTimeout%
if errorlevel neq 0 (
    set exit_code=%errorlevel%
)
echo "Finished running tests: exit_code=%exit_code%"
exit /b %exit_code%
