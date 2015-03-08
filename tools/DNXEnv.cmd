@echo off

:: Set environment variables
set "DNXRoot=%~dp0%"
set "DNXRoot=%DNXRoot:~0,-7%"
set "DNXApps=%DNXRoot%\apps"
set "DNXSource=%DNXRoot%\src"
set "DNXTest=%DNXRoot%\test"
set "DNXTools=%DNXRoot%\tools"
set CustomBeforeDNXTargets=%~dp0K.Extensions.settings.targets

if defined ProgramFiles(x86) (
    set "DNXProgramFiles=%ProgramFiles(x86)%"
) else (
    set "DNXProgramFiles=%ProgramFiles%"
)

if exist "%DNXProgramFiles%\Microsoft Visual Studio 11.0" (
    set "DNXVSInstallDir=%DNXProgramFiles%\Microsoft Visual Studio 11.0"
    set "VisualStudioVersion=11.0"
) else (
    set "DNXVSInstallDir=%DNXProgramFiles%\Microsoft Visual Studio 10.0"
)

for /F "tokens=3 delims= " %%i in ('REG QUERY "HKLM\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full"  /v InstallPath') do set NDPInstallPath=%%i

set "PATH=%PATH%;%NdpInstallPath%"
set "PATH=%PATH%;%DNXProgramFiles%\Microsoft SDKs\Windows\v7.0A\bin\NETFX 4.0 Tools"
set "PATH=%PATH%;%DNXProgramFiles%\Microsoft SDKs\Windows\v7.0A\bin"
set "PATH=%PATH%;%DNXProgramFiles%\Microsoft SDKs\Windows\v8.0A\bin\NETFX 4.0 Tools"
set "PATH=%PATH%;%DNXProgramFiles%\Microsoft SDKs\Windows\v8.0A\bin"
set "PATH=%PATH%;%DNXVSInstallDir%\Common7\IDE"
set "PATH=%PATH%;%DNXTools%"
set "PATH=%PATH%;%DNXTools%\SubmitTools"

:: Configure WinDiff as the comparison tool for VS2010 and VS2008.  You can still invoke Odd directly to view changes,
:: but KKV and VS will use WinDiff.  If desired, you can override these reg keys to point to Odd (or any other tool)
:: in the setenv.cmd in your developer directory.
setlocal
set "v9key=HKCU\Software\Microsoft\VisualStudio\9.0\TeamFoundation\SourceControl\DiffTools\.*\Compare"
set "v10key=HKCU\Software\Microsoft\VisualStudio\10.0\TeamFoundation\SourceControl\DiffTools\.*\Compare"
set "windiff=%DNXProgramFiles%\Microsoft SDKs\Windows\v7.0A\bin\WinDiff.Exe"
:: Set both v9 and v10 keys, since kkv has not been updated for Dev10 and still loads some values from the v9 keys.
reg add "%v9key%" /v Command /d "%windiff%" /f > nul
reg add "%v9key%" /v Arguments /d "%%1 %%2" /f > nul
reg add "%v10key%" /v Command /d "%windiff%" /f > nul
reg add "%v10key%" /v Arguments /d "%%1 %%2" /f > nul
endlocal

:: Invoke developer-specific setenv.cmd, if it exists
if exist "%DNXRoot%\developer\%USERNAME%\setenv.cmd" call "%DNXRoot%\Developer\%USERNAME%\setenv.cmd"
