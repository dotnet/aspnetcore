# Check if Elevated
$WindowsIdentity = [system.security.principal.windowsidentity]::GetCurrent()
$Principal = New-Object System.Security.Principal.WindowsPrincipal($WindowsIdentity)
$AdminRole = [System.Security.Principal.WindowsBuiltInRole]::Administrator
if (!$Principal.IsInRole($AdminRole)) {
    throw "This script requires an Admin shell."
}

if(!(Get-Command choco -ErrorAction SilentlyContinue)) {
    iex ((New-Object System.Net.WebClient).DownloadString('https://chocolatey.org/install.ps1'))
}

choco install -y jdk10 -params "source=false"

# Install GPG as well, for Maven signature generation
choco install -y gpg4win