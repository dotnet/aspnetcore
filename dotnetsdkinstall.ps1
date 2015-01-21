$tempPath = Join-Path $env:TEMP "dotnetsdkinstall"
$dotnetsdkPs1Path = Join-Path $tempPath "dotnetsdk.ps1"
$dotnetsdkCmdPath = Join-Path $tempPath "dotnetsdk.cmd"

Write-Host "Using temporary directory: $tempPath"
if (!(Test-Path $tempPath)) { md $tempPath | Out-Null }


$webClient = New-Object System.Net.WebClient
Write-Host "Downloading dotnetsdk.ps1 to $dotnetsdkPs1Path"
$webClient.DownloadFile('https://raw.githubusercontent.com/aspnet/Home/master/dotnetsdk.ps1', $dotnetsdkPs1Path)
Write-Host "Downloading dotnetsdk.cmd to $dotnetsdkCmdPath"
$webClient.DownloadFile('https://raw.githubusercontent.com/aspnet/Home/master/dotnetsdk.cmd', $dotnetsdkCmdPath)
Write-Host "Installing dotnetsdk"
& $dotnetsdkCmdPath setup