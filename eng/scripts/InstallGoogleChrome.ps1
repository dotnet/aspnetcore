$InstallerPath = "$env:Temp\chrome_installer.exe";
& $PSScriptRoot\Download.ps1 "https://dl.google.com/chrome/install/latest/chrome_installer.exe" $InstallerPath
Start-Process -FilePath $InstallerPath -Args "/silent /install" -Verb RunAs -Wait;
Remove-Item $InstallerPath
