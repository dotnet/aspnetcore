REM copy the AspNet.Loader.dll to bin folder
md wwwroot\bin

SET ASPNETLOADER_PACKAGE_BASEPATH=%USERPROFILE%\\.dnx\\packages\Microsoft.AspNet.Loader.IIS.Interop
REM figure out the path of AspNet.Loader.dll
FOR /F %%j IN ('dir /b /o:D %ASPNETLOADER_PACKAGE_BASEPATH%\*') do (SET AspNetLoaderPath=%ASPNETLOADER_PACKAGE_BASEPATH%\%%j\tools\AspNet.Loader.dll)
echo Found AspNetLoader.dll at %AspNetLoaderPath%. Copying to bin\
copy %AspNetLoaderPath% wwwroot\bin\
