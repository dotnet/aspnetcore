@Echo off

set KVM_CMD_PATH_FILE="%USERPROFILE%\.k\temp-set-envvars.cmd"

PowerShell -NoProfile -NoLogo -ExecutionPolicy unrestricted -Command "[System.Threading.Thread]::CurrentThread.CurrentCulture = ''; [System.Threading.Thread]::CurrentThread.CurrentUICulture = '';$CmdPathFile='%KVM_CMD_PATH_FILE%';& '%~dp0kvm.ps1' %*"

IF EXIST "%KVM_CMD_PATH_FILE%" (
  CALL "%KVM_CMD_PATH_FILE%"
  DEL "%KVM_CMD_PATH_FILE%"
)
