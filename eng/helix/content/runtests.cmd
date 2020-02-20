@echo off
REM Disable "!Foo!" expansions because they break the filter syntax
setlocal disableextensions

set target=%1
set targetFrameworkIdentifier=%2
set sdkVersion=%3
set runtimeVersion=%4
set helixQueue=%5
set arch=%6
set quarantined=%7

set DOTNET_HOME=%HELIX_CORRELATION_PAYLOAD%\sdk
set DOTNET_ROOT=%DOTNET_HOME%\%arch%
set DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
set DOTNET_MULTILEVEL_LOOKUP=0
set DOTNET_CLI_HOME=%HELIX_CORRELATION_PAYLOAD%\home

set PATH=%DOTNET_ROOT%;%PATH%;%HELIX_CORRELATION_PAYLOAD%\node\bin

powershell.exe -NoProfile -ExecutionPolicy unrestricted -Command "[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; &([scriptblock]::Create((Invoke-WebRequest -useb 'https://dot.net/v1/dotnet-install.ps1'))) -Architecture %arch% -Version %sdkVersion% -InstallDir %DOTNET_ROOT%"
powershell.exe -NoProfile -ExecutionPolicy unrestricted -Command "[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; &([scriptblock]::Create((Invoke-WebRequest -useb 'https://dot.net/v1/dotnet-install.ps1'))) -Architecture %arch% -Runtime dotnet -Version %runtimeVersion% -InstallDir %DOTNET_ROOT%"

if EXIST ".\Microsoft.AspNetCore.App" (
    echo "Found Microsoft.AspNetCore.App, copying to %DOTNET_ROOT%\shared\Microsoft.AspNetCore.App\%runtimeVersion%"
    xcopy /i /y ".\Microsoft.AspNetCore.App" %DOTNET_ROOT%\shared\Microsoft.AspNetCore.App\%runtimeVersion%\
)

echo "Current Directory: %HELIX_WORKITEM_ROOT%"
set HELIX=%helixQueue%
set HELIX_DIR=%HELIX_WORKITEM_ROOT%
set NUGET_FALLBACK_PACKAGES=%HELIX_DIR%
set NUGET_RESTORE=%HELIX_DIR%\nugetRestore
echo "Setting HELIX_DIR: %HELIX_DIR%"
echo Creating nuget restore directory: %NUGET_RESTORE%
mkdir %NUGET_RESTORE%
mkdir logs

dir

%DOTNET_ROOT%\dotnet vstest %target% -lt >discovered.txt
find /c "Exception thrown" discovered.txt
REM "ERRORLEVEL is not %ERRORLEVEL%" https://blogs.msdn.microsoft.com/oldnewthing/20080926-00/?p=20743/
if not errorlevel 1 (
    echo Exception thrown during test discovery. 1>&2
    type discovered.txt 1>&2
    exit /b 1
)

set exit_code=0

set NONQUARANTINE_FILTER="Flaky:All!=true&Flaky:Helix:All!=true&Flaky:Helix:Queue:All!=true&Flaky:Helix:Queue:%HELIX%!=true"
set QUARANTINE_FILTER="Flaky:All=true|Flaky:Helix:All=true|Flaky:Helix:Queue:All=true|Flaky:Helix:Queue:%HELIX%=true"
if %quarantined%==true (
    echo Running quarantined tests.
    %DOTNET_ROOT%\dotnet vstest %target% --logger:xunit --TestCaseFilter:%QUARANTINE_FILTER%
    if errorlevel 1 (
        echo Failure in flaky test 1>&2
        REM DO NOT EXIT and DO NOT SET EXIT_CODE to 1
    )
) else (
    REM We need to specify all possible Flaky filters that apply to this environment, because the flaky attribute
    REM only puts the explicit filter traits the user provided in
    REM Filter syntax: https://github.com/Microsoft/vstest-docs/blob/master/docs/filter.md
    echo Running non-quarantined tests.
    %DOTNET_ROOT%\dotnet vstest %target% --logger:xunit --TestCaseFilter:%NONQUARANTINE_FILTER%
    if errorlevel 1 (
        echo Failure in non-flaky test 1>&2
        set exit_code=1
        REM DO NOT EXIT
    )
)

echo "Copying TestResults\TestResults.xml to ."
copy TestResults\TestResults.xml testResults.xml

exit /b %exit_code%

