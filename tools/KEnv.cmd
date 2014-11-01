@echo off

:: Set environment variables
set "KRoot=%~dp0%"
set "KRoot=%KRoot:~0,-7%"
set "KApps=%KRoot%\apps"
set "KSource=%KRoot%\src"
set "KTest=%KRoot%\test"
set "KTools=%KRoot%\tools"
set CustomBeforeKTargets=%~dp0K.Extensions.settings.targets

if defined ProgramFiles(x86) (
    set "KProgramFiles=%ProgramFiles(x86)%"
) else (
    set "KProgramFiles=%ProgramFiles%"
)

if exist "%KProgramFiles%\Microsoft Visual Studio 11.0" (
    set "KVSInstallDir=%KProgramFiles%\Microsoft Visual Studio 11.0"
    set "VisualStudioVersion=11.0"
) else (
    set "KVSInstallDir=%KProgramFiles%\Microsoft Visual Studio 10.0"
)

for /F "tokens=3 delims= " %%i in ('REG QUERY "HKLM\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full"  /v InstallPath') do set NDPInstallPath=%%i

set "PATH=%PATH%;%NdpInstallPath%"
set "PATH=%PATH%;%KProgramFiles%\Microsoft SDKs\Windows\v7.0A\bin\NETFX 4.0 Tools"
set "PATH=%PATH%;%KProgramFiles%\Microsoft SDKs\Windows\v7.0A\bin"
set "PATH=%PATH%;%KProgramFiles%\Microsoft SDKs\Windows\v8.0A\bin\NETFX 4.0 Tools"
set "PATH=%PATH%;%KProgramFiles%\Microsoft SDKs\Windows\v8.0A\bin"
set "PATH=%PATH%;%KVSInstallDir%\Common7\IDE"
set "PATH=%PATH%;%KTools%"
set "PATH=%PATH%;%KTools%\SubmitTools"

:: Configure WinDiff as the comparison tool for VS2010 and VS2008.  You can still invoke Odd directly to view changes,
:: but KKV and VS will use WinDiff.  If desired, you can override these reg keys to point to Odd (or any other tool)
:: in the setenv.cmd in your developer directory.
setlocal
set "v9key=HKCU\Software\Microsoft\VisualStudio\9.0\TeamFoundation\SourceControl\DiffTools\.*\Compare"
set "v10key=HKCU\Software\Microsoft\VisualStudio\10.0\TeamFoundation\SourceControl\DiffTools\.*\Compare"
set "windiff=%KProgramFiles%\Microsoft SDKs\Windows\v7.0A\bin\WinDiff.Exe"
:: Set both v9 and v10 keys, since kkv has not been updated for Dev10 and still loads some values from the v9 keys.
reg add "%v9key%" /v Command /d "%windiff%" /f > nul
reg add "%v9key%" /v Arguments /d "%%1 %%2" /f > nul
reg add "%v10key%" /v Command /d "%windiff%" /f > nul
reg add "%v10key%" /v Arguments /d "%%1 %%2" /f > nul
endlocal

:: Invoke developer-specific setenv.cmd, if it exists
if exist "%KRoot%\developer\%USERNAME%\setenv.cmd" call "%KRoot%\Developer\%USERNAME%\setenv.cmd"
