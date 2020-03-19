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
echo "Checking for Microsoft.AspNetCore.App"
if EXIST ".\Microsoft.AspNetCore.App" (
    echo "Found Microsoft.AspNetCore.App, copying to %DOTNET_ROOT%\shared\Microsoft.AspNetCore.App\%runtimeVersion%"
    xcopy /i /y ".\Microsoft.AspNetCore.App" %DOTNET_ROOT%\shared\Microsoft.AspNetCore.App\%runtimeVersion%\

    echo "Adding current directory to nuget sources: %HELIX_WORKITEM_ROOT%"
    dotnet nuget add source %HELIX_WORKITEM_ROOT%
    dotnet nuget add source https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet5/nuget/v3/index.json
    dotnet nuget list source
    dotnet tool install dotnet-ef --global --version %$efVersion%

    set PATH=!PATH!;%DOTNET_CLI_HOME%\.dotnet\tools
)

echo "Current Directory: %HELIX_WORKITEM_ROOT%"
set HELIX=%$helixQueue%
set HELIX_DIR=%HELIX_WORKITEM_ROOT%
set NUGET_FALLBACK_PACKAGES=%HELIX_DIR%
set NUGET_RESTORE=%HELIX_DIR%\nugetRestore
set DotNetEfFullPath=%HELIX_DIR%\nugetRestore\dotnet-ef\%$efVersion%\tools\netcoreapp3.1\any\dotnet-ef.exe
echo "Set DotNetEfFullPath: %DotNetEfFullPath%"
echo "Setting HELIX_DIR: %HELIX_DIR%"
echo Creating nuget restore directory: %NUGET_RESTORE%
mkdir %NUGET_RESTORE%
mkdir logs

REM "Rename default.runner.json to xunit.runner.json if there is not a custom one from the project"
if not EXIST ".\xunit.runner.json" (
    copy default.runner.json xunit.runner.json
)

dir

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

REM Disable "!Foo!" expansions because they break the filter syntax
setlocal disabledelayedexpansion
set NONQUARANTINE_FILTER="Quarantined!=true"
set QUARANTINE_FILTER="Quarantined=true"
if %$quarantined%==true (
    echo Running quarantined tests.
    %DOTNET_ROOT%\dotnet vstest %$target% --logger:xunit --TestCaseFilter:%QUARANTINE_FILTER%
    if errorlevel 1 (
        echo Failure in quarantined test 1>&2
        REM DO NOT EXIT and DO NOT SET EXIT_CODE to 1
    )
) else (
    REM Filter syntax: https://github.com/Microsoft/vstest-docs/blob/master/docs/filter.md
    echo Running non-quarantined tests.
    %DOTNET_ROOT%\dotnet vstest %$target% --logger:xunit --TestCaseFilter:%NONQUARANTINE_FILTER%
    if errorlevel 1 (
        echo Failure in non-quarantined test 1>&2
        set exit_code=1
        REM DO NOT EXIT
    )
)

echo "Copying TestResults\TestResults.xml to ."
copy TestResults\TestResults.xml testResults.xml

exit /b %exit_code%

