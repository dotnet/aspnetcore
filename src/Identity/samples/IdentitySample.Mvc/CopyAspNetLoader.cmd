REM copy the AspNet.Loader.dll to bin folder
md bin

REM figure out the path of AspNet.Loader.dll
FOR /F %%j IN ('dir /b /o:-d ..\..\packages\Microsoft.AspNetCore.Loader.IIS.Interop*') do (SET AspNetLoaderPath=..\..\packages\%%j\tools\AspNet.Loader.dll)
echo Found AspNetLoader.dll at %AspNetLoaderPath%. Copying to bin\
copy %AspNetLoaderPath% bin\