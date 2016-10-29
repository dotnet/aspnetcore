@ECHO OFF
:again
if not "%1" == "" (
    echo "Deleting %1\tools"
    rmdir /s /q %1\tools
)

mkdir %1\tools
copy ..\..\src\Microsoft.DotNet.Watcher.Tools\tools\*.targets %1\tools