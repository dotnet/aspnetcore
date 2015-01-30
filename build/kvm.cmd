@Echo off

PowerShell -NoProfile -NoLogo -ExecutionPolicy unrestricted -Command "[System.Threading.Thread]::CurrentThread.CurrentCulture = ''; [System.Threading.Thread]::CurrentThread.CurrentUICulture = '';& '%~dp0kvm.ps1' %*"

IF EXIST "%USERPROFILE%\.k\temp-set-envvars.cmd" (
  CALL "%USERPROFILE%\.k\temp-set-envvars.cmd"
  DEL "%USERPROFILE%\.k\temp-set-envvars.cmd"
)
