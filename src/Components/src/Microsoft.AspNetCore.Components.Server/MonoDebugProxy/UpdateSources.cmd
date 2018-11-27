@echo off
echo |----
echo | Copying the ws-proxy sources here is a temporary step until ws-proxy is
echo | distributed as a NuGet package.
echo | ...
echo | Instead of dealing with Git submodules, this script simply fetches the
echo | latest sources so they can be built directly inside this project (hence
echo | we don't have to publish our own separate package for this).
echo | ...
echo | When updating, you'll need to re-apply any patches we've made manually.
echo |----
@echo on

cd /D "%~dp0"
rmdir /s /q ws-proxy
git clone https://github.com/kumpera/ws-proxy.git
rmdir /s /q ws-proxy\.git
del ws-proxy\*.csproj
del ws-proxy\*.sln
del ws-proxy\Program.cs
