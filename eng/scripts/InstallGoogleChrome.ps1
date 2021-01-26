$InstallerPath = "$env:Temp\chrome_installer.exe";
& $PSScriptRoot\Download.ps1 "http://dl.google.com/chrome/install/375.126/chrome_installer.exe" $InstallerPath
Start-Process -FilePath $InstallerPath -Args "/silent /install" -Verb RunAs -Wait;
Remove-Item $InstallerPath
