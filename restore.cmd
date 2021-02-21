@ECHO OFF
SETLOCAL
PowerShell -NoProfile -NoLogo -ExecutionPolicy ByPass -Command "[System.Threading.Thread]::CurrentThread.CurrentCulture = ''; [System.Threading.Thread]::CurrentThread.CurrentUICulture = '';& '%~dp0\eng\build.ps1' -all -nobuild -restore %*; exit $LASTEXITCODE"
