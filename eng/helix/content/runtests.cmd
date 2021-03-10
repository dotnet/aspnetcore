@ECHO OFF
SETLOCAL
PowerShell -NoProfile -NoLogo -ExecutionPolicy Bypass -Command "[System.Threading.Thread]::CurrentThread.CurrentCulture = ''; [System.Threading.Thread]::CurrentThread.CurrentUICulture = ''; try { & '%~dp0runtests.ps1' %*; exit $LASTEXITCODE } catch { write-host $_; exit 1 }"
SET exit_code=%ERRORLEVEL%
ECHO runtests.cmd completed
EXIT /b %exit_code%
