net stop was /Y
.\build.cmd
cp "C:\Users\jukotali\code\aspnetcore\artifacts\bin\AspNetCoreModuleShim\x64\Debug\*" "C:\Program Files\IIS\Asp.Net Core Module\V2\" -Force
net start w3svc /Y