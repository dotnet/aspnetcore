@echo off
setlocal

set "searchRoot=%~dp0..\..\..\artifacts\bin\AspNetCoreFuzzing"
for %%I in ("%searchRoot%") do set "searchRoot=%%~fI"

for /f "delims=" %%F in ('dir /s /b "%searchRoot%\AspNetCoreFuzzing.exe" 2^>nul') do (
    set "exePath=%%~fF"
    goto :found
)

echo AspNetCoreFuzzing.exe not found under "%searchRoot%"
exit /b 1

:found
"%exePath%" prepare-onefuzz deployment
