$tempPath = Join-Path $env:TEMP "dnvminstall"
$kvmPs1Path = Join-Path $tempPath "dnvm.ps1"
$kvmCmdPath = Join-Path $tempPath "dnvm.cmd"

Write-Host "Using temporary directory: $tempPath"
if (!(Test-Path $tempPath)) { md $tempPath | Out-Null }


$webClient = New-Object System.Net.WebClient
Write-Host "Downloading DNVM.ps1 to $dnvmPs1Path"
$webClient.DownloadFile('https://raw.githubusercontent.com/aspnet/Home/dev/dnvm.ps1', $kvmPs1Path)
Write-Host "Downloading DNVM.cmd to $dnvmCmdPath"
$webClient.DownloadFile('https://raw.githubusercontent.com/aspnet/Home/dev/dnvm.cmd', $kvmCmdPath)
Write-Host "Installing DNVM"
& $kvmCmdPath setup