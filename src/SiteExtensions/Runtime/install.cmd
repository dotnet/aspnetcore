FOR /R %%x IN (*.nupkg_) DO REN "%%x" "*.nupkg"


SET DOTNET=D:\Program Files (x86)\dotnet
SET RUNTIMES=%DOTNET%\shared\Microsoft.NETCore.App

IF "%ASPNETCORE_COPY_EXISTING_RUNTIMES%" NEQ "1" EXIT /b 0

robocopy "%DOTNET%" "." /E /XC /XN /XO /NFL /NDL ^
    /XD "%DOTNET%\sdk" ^
    /XD "%RUNTIMES%\1.0.8" ^
    /XD "%RUNTIMES%\1.1.5" ^
    /XD "%RUNTIMES%\2.0.3"

IF %errorlevel% geq 8 EXIT /b 1
EXIT /b 0