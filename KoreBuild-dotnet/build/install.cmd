@Echo off

for /f "delims=" %%i in ('PowerShell -NoProfile -NoLogo -ExecutionPolicy unrestricted -Command "[System.IO.Path]::GetTempFileName()"') do set INSTALL_CMD_PATH_FILE="%%i.cmd"

PowerShell -NoProfile -NoLogo -ExecutionPolicy unrestricted -Command "[System.Threading.Thread]::CurrentThread.CurrentCulture = ''; [System.Threading.Thread]::CurrentThread.CurrentUICulture = '';$CmdPathFile='%INSTALL_CMD_PATH_FILE%';& '%~dp0install.ps1' %*"

IF EXIST %INSTALL_CMD_PATH_FILE% (
  CALL %INSTALL_CMD_PATH_FILE%
  DEL %INSTALL_CMD_PATH_FILE%
)
