@echo off
REM Disable "!Foo!" expansions because they break the filter syntax
setlocal disableextensions

REM Use '$' as a variable name prefix to avoid MSBuild variable collisions with these variables
set $target=%1
set $sdkVersion=%2
set $runtimeVersion=%3
set $helixQueue=%4
set $arch=%5
set $quarantined=%6

set DOTNET_HOME=%HELIX_CORRELATION_PAYLOAD%\sdk
set DOTNET_ROOT=%DOTNET_HOME%\%$arch%
set DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
set DOTNET_MULTILEVEL_LOOKUP=0
set DOTNET_CLI_HOME=%HELIX_CORRELATION_PAYLOAD%\home

set PATH=%DOTNET_ROOT%;%PATH%;%HELIX_CORRELATION_PAYLOAD%\node\bin

powershell.exe -NoProfile -ExecutionPolicy unrestricted -Command "[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; &([scriptblock]::Create((Invoke-WebRequest -useb 'https://dot.net/v1/dotnet-install.ps1'))) -Architecture %$arch% -Version %$sdkVersion% -InstallDir %DOTNET_ROOT%"
powershell.exe -NoProfile -ExecutionPolicy unrestricted -Command "[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; &([scriptblock]::Create((Invoke-WebRequest -useb 'https://dot.net/v1/dotnet-install.ps1'))) -Architecture %$arch% -Runtime dotnet -Version %$runtimeVersion% -InstallDir %DOTNET_ROOT%"

set HELIX=%$helixQueue%

%DOTNET_ROOT%\dotnet vstest %$target% -lt >discovered.txt
find /c "Exception thrown" discovered.txt
REM "ERRORLEVEL is not %ERRORLEVEL%" https://blogs.msdn.microsoft.com/oldnewthing/20080926-00/?p=20743/
if not errorlevel 1 (
    echo Exception thrown during test discovery. 1>&2
    type discovered.txt 1>&2
    exit /b 1
)

set exit_code=0

if %$quarantined%==True (
    set %$quarantined=true
)

set NONQUARANTINE_FILTER="Flaky:All!=true&Flaky:Helix:All!=true&Flaky:Helix:Queue:All!=true&Flaky:Helix:Queue:%HELIX%!=true"
set QUARANTINE_FILTER="Flaky:All=true|Flaky:Helix:All=true|Flaky:Helix:Queue:All=true|Flaky:Helix:Queue:%HELIX%=true"
if %$quarantined%==true (
    echo Running quarantined tests.
    %DOTNET_ROOT%\dotnet vstest %$target% --logger:xunit --TestCaseFilter:%QUARANTINE_FILTER%
    if errorlevel 1 (
        echo Failure in flaky test 1>&2
        REM DO NOT EXIT and DO NOT SET EXIT_CODE to 1
    )
) else (
    REM We need to specify all possible Flaky filters that apply to this environment, because the flaky attribute
    REM only puts the explicit filter traits the user provided in
    REM Filter syntax: https://github.com/Microsoft/vstest-docs/blob/master/docs/filter.md
    echo Running non-quarantined tests.
    %DOTNET_ROOT%\dotnet vstest %$target% --logger:xunit --TestCaseFilter:%NONQUARANTINE_FILTER%
    if errorlevel 1 (
        echo Failure in non-flaky test 1>&2
        set exit_code=1
        REM DO NOT EXIT
    )
)

echo "Copying TestResults\TestResults.xml to ."
copy TestResults\TestResults.xml testResults.xml

exit /b %exit_code%

