$ErrorActionPreference = 'Continue'

function _kill($processName) {
    try {
        # Redirect stderr to stdout to avoid big red blocks of output in Azure Pipeline logging
        # when there are no instances of the process
        & cmd /c "taskkill /T /F /IM ${processName} 2>&1"
    } catch {
        Write-Host "Failed to kill ${processName}: $_"
    }
}

function _killJavaInstances() {
    $_javaProcesses = Get-Process java -ErrorAction SilentlyContinue |
        Where-Object { $_.Path -like "$env:JAVA_HOME*" };
    foreach($_javaProcess in $_javaProcesses) {
        try {
            Stop-Process $_javaProcess
        } catch {
            Write-Host "Failed to kill java process: $_javaProcess"
        }
    }
}

function _killSeleniumTrackedProcesses() {
    $files = Get-ChildItem $env:SeleniumProcessTrackingFolder -ErrorAction SilentlyContinue;
    # PID files have a format of <<pid>>.<<guid>>.pid
    $pids = $files |
        Where-Object { $_.Name -match "([0-9]+)\..*?.pid"; } |
        Foreach-Object { $Matches[1] };

    foreach ($currentPid in $pids) {
        try {
            & cmd /c "taskkill /T /F /PID $currentPid 2>&1"
        } catch {
            Write-Host "Failed to kill process: $currentPid"
        }
    }
}

_kill dotnet.exe
_kill testhost.exe
_kill iisexpress.exe
_kill iisexpresstray.exe
_kill w3wp.exe
_kill msbuild.exe
_kill vbcscompiler.exe
_kill vctip.exe
_kill h2spec.exe
_kill WerFault.exe
_killJavaInstances
_killSeleniumTrackedProcesses

# Special case these. When testing with -ci locally, you typically don't actually want to kill your browser or git command line
if ($env:TF_BUILD) {
    _kill chrome.exe
    _kill git.exe
}

if (Get-Command iisreset -ErrorAction ignore) {
    iisreset /restart
}
Stop-Service w3svc -NoWait -ErrorAction Ignore

exit 0
