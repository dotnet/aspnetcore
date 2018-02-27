Invoke-WebRequest -useb 'https://dot.net/v1/dotnet-install.ps1' -OutFile .\build\dotnet-install.ps1
.\build\dotnet-install.ps1 -channel Current -version 2.1.3 -InstallDir dotnetsdk
dotnet --version
dotnet pack Blazor.sln --configuration Release
