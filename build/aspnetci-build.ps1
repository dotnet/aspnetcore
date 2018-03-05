Invoke-WebRequest -useb 'https://dot.net/v1/dotnet-install.ps1' -OutFile .\build\dotnet-install.ps1
.\build\dotnet-install.ps1 -channel Current -version 2.1.3 -InstallDir dotnetsdk
dotnet --version
dotnet pack Blazor.sln --configuration Release
cmd /c "C:\Program Files (x86)\Microsoft Visual Studio\aspnetci\Enterprise\MSBuild\15.0\Bin\MSBuild.exe" BlazorTooling.sln /t:Restore
cmd /c "C:\Program Files (x86)\Microsoft Visual Studio\aspnetci\Enterprise\MSBuild\15.0\Bin\MSBuild.exe" BlazorTooling.sln /p:DeployExtension=false
