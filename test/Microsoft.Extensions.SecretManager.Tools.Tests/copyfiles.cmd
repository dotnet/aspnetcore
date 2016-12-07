@ECHO OFF
:again
if not "%1" == "" (
    echo "Deleting %1\toolassets"
    rmdir /s /q %1\toolassets
)

mkdir %1\toolassets
copy ..\..\src\Microsoft.Extensions.SecretManager.Tools\toolassets\*.targets %1\toolassets