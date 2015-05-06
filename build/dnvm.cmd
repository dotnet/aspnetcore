@Echo off

set DNVM_CMD_PATH_FILE="%USERPROFILE%\.dnx\temp-set-envvars.cmd"

PowerShell -NoProfile -NoLogo -ExecutionPolicy unrestricted -Command "[System.Threading.Thread]::CurrentThread.CurrentCulture = ''; [System.Threading.Thread]::CurrentThread.CurrentUICulture = '';$CmdPathFile='%DNVM_CMD_PATH_FILE%';& '%~dp0dnvm.ps1' %*"

IF EXIST %DNVM_CMD_PATH_FILE% (
  CALL %DNVM_CMD_PATH_FILE%
  DEL %DNVM_CMD_PATH_FILE%
)
