@ECHO OFF
:again
if not "%1" == "" (
    echo "Deleting %1\TestProjects"
    rmdir /s /q %1\TestProjects
    echo "Deleting %1\toolassets"
    rmdir /s /q %1\toolassets
)

mkdir %1\toolassets
copy ..\..\src\Microsoft.DotNet.Watcher.Tools\toolassets\*.targets %1\toolassets