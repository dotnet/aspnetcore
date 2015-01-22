@Echo off

PowerShell -NoProfile -NoLogo -ExecutionPolicy unrestricted -Command "[System.Threading.Thread]::CurrentThread.CurrentCulture = ''; [System.Threading.Thread]::CurrentThread.CurrentUICulture = '';& '%~dp0dotnetsdk.ps1' %*"

IF EXIST "%USERPROFILE%\.dotnet\temp-set-envvars.cmd" (
  CALL "%USERPROFILE%\.dotnet\temp-set-envvars.cmd"
  DEL "%USERPROFILE%\.dotnet\temp-set-envvars.cmd"
)
