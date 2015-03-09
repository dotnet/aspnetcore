SET DNX_HOME=%USERPROFILE%\.dnx\

REM copy the AspNet.Loader.dll to the bin folder
call CopyAspNetLoader.cmd

"%ProgramFiles(x86)%\iis Express\iisexpress.exe" /port:5001 /path:"%cd%\wwwroot"
