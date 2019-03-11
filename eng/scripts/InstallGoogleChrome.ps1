$InstallerPath = "$env:Temp\chrome_installer.exe";
Invoke-WebRequest "http://dl.google.com/chrome/install/375.126/chrome_installer.exe" -OutFile $InstallerPath;
Start-Process -FilePath $InstallerPath -Args "/silent /install" -Verb RunAs -Wait;
Remove-Item $InstallerPath
