@Echo off

for /f "delims=" %%i in ('PowerShell -NoProfile -NoLogo -ExecutionPolicy unrestricted -Command "[System.IO.Path]::GetTempFileName()"') do set DNVM_CMD_PATH_FILE=%%i

PowerShell -NoProfile -NoLogo -ExecutionPolicy unrestricted -Command "[System.Threading.Thread]::CurrentThread.CurrentCulture = ''; [System.Threading.Thread]::CurrentThread.CurrentUICulture = '';$CmdPathFile='%DNVM_CMD_PATH_FILE%';& '%~dp0dnvm.ps1' %*"

IF EXIST %DNVM_CMD_PATH_FILE% (
  CALL %DNVM_CMD_PATH_FILE%
  DEL %DNVM_CMD_PATH_FILE%
)
