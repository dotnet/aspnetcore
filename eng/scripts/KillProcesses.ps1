$ErrorActionPreference = 'Continue'

function _kill($processName) {
    try {
        & cmd /c "taskkill /T /F /IM $processName 2>&1"
    } catch {
        Write-Host "Failed to kill $processName: $_"
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
iisreset /restart

exit 0
