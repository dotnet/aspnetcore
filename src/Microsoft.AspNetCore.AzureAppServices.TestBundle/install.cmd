SET DOTNET=D:\Program Files (x86)\dotnet
SET RUNTIMES=%DOTNET%\shared\Microsoft.NETCore.App
robocopy "%DOTNET%" "." /E /XC /XN /XO /NFL /NDL ^
    /XD "%DOTNET%\sdk" ^
    /XD "%RUNTIMES%\1.0.3" ^
    /XD "%RUNTIMES%\1.0.4" ^
    /XD "%RUNTIMES%\1.1.0" ^
    /XD "%RUNTIMES%\1.1.0-preview1-001100-00" ^
    /XD "%RUNTIMES%\1.1.1" ^
    /XD "%RUNTIMES%\2.0.0-preview1-002111-00" ^
    /XD "%RUNTIMES%\2.0.0-preview2-25407-01"

if %errorlevel% geq 8 exit /b 1
exit /b 0