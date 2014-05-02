@Echo off

PowerShell -NoProfile -NoLogo -ExecutionPolicy unrestricted -Command "[System.Threading.Thread]::CurrentThread.CurrentCulture = ''; [System.Threading.Thread]::CurrentThread.CurrentUICulture = '';& '%~dp0kvm.ps1' %*"

IF EXIST "%USERPROFILE%\.kre\run-once.cmd" (
  CALL "%USERPROFILE%\.kre\run-once.cmd"
  DEL "%USERPROFILE%\.kre\run-once.cmd"
)
