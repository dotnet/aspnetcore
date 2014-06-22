$tempPath = Join-Path $env:TEMP "kvminstall"
$kvmPs1Path = Join-Path $tempPath "kvm.ps1"
$kvmCmdPath = Join-Path $tempPath "kvm.cmd"

Write-Host "Using temporary directory: $tempPath"
if (!(Test-Path $tempPath)) { md $tempPath | Out-Null }


$webClient = New-Object System.Net.WebClient
Write-Host "Downloading KVM.ps1 to $kvmPs1Path"
$webClient.DownloadFile('https://raw.githubusercontent.com/aspnet/Home/master/kvm.ps1', $kvmPs1Path)
Write-Host "Downloading KVM.cmd to $kvmCmdPath"
$webClient.DownloadFile('https://raw.githubusercontent.com/aspnet/Home/master/kvm.cmd', $kvmCmdPath)
Write-Host "Installing KVM"
& $kvmCmdPath setup