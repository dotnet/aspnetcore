@ECHO OFF

SET _TAIL=%*
CALL SET _TAIL=%%_TAIL:*%1=%%

SET POWERSHELL=%windir%\System32\WindowsPowerShell\v1.0\powershell.exe

rem Force 64bit powershell
if /i "%PROCESSOR_ARCHITEW6432%" EQU "AMD64" SET POWERSHELL=%windir%\sysnative\WindowsPowerShell\v1.0\powershell.exe
echo "PS: Running '%~dp0%1' %_TAIL%"
%POWERSHELL% -NoProfile -NoLogo -ExecutionPolicy unrestricted -Command "[System.Threading.Thread]::CurrentThread.CurrentCulture = ''; [System.Threading.Thread]::CurrentThread.CurrentUICulture = ''; try { & '%~dp0%1' %_TAIL%; exit $LASTEXITCODE } catch { write-host $_; exit 1 }"
