@ECHO OFF

SET _TAIL=%*
CALL SET _TAIL=%%_TAIL:*%1=%%

PowerShell -NoProfile -NoLogo -ExecutionPolicy unrestricted -Command "[System.Threading.Thread]::CurrentThread.CurrentCulture = ''; [System.Threading.Thread]::CurrentThread.CurrentUICulture = ''; try { & '%~dp0%1' %_TAIL%; exit $LASTEXITCODE } catch { write-host $_; exit 1 }"
