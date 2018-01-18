SET DOTNET=D:\Program Files (x86)\dotnet

for /R %%x in (*.nupkg_) do ren "%%x" "*.nupkg"

dotnet msbuild /version
