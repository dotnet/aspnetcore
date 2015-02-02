if(!$Branch) { Set-Variable Branch "release" }
if(!$Repository) { Set-Variable Repository "aspnet/Home" }

$tempPath = Join-Path $env:TEMP "kvminstall"
$kvmPs1Path = Join-Path $tempPath "kvm.ps1"
$kvmCmdPath = Join-Path $tempPath "kvm.cmd"

Write-Host "Using temporary directory: $tempPath"
if (!(Test-Path $tempPath)) { md $tempPath | Out-Null }


$webClient = New-Object System.Net.WebClient
Write-Host "Downloading KVM.ps1 to $kvmPs1Path"
$webClient.DownloadFile("https://raw.githubusercontent.com/$Repository/$Branch/kvm.ps1", $kvmPs1Path)
Write-Host "Downloading KVM.cmd to $kvmCmdPath"
$webClient.DownloadFile("https://raw.githubusercontent.com/$Repository/$Branch/kvm.cmd", $kvmCmdPath)
Write-Host "Installing KVM"
& $kvmCmdPath setup