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
            Stop-Process $proc
        } catch {
            Write-Host "Failed to kill java process: $proc"
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
_kill git.exe
_kill vctip.exe
_kill chrome.exe
_kill h2spec.exe
_kill WerFault.exe
_killJavaInstances

if (Get-Command iisreset -ErrorAction ignore) {
    iisreset /restart
}
Stop-Service w3svc -NoWait -ErrorAction Ignore

exit 0
