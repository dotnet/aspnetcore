[CmdletBinding()]
param(
    [string]$buildAgentFolder="C:\BuildAgent",
    
    [string]$teamAgentServiceAccountName="redmond\asplab",
    
    [Parameter(Mandatory=$true)]
    [string]$teamAgentServiceAccountPassword
)

$wrapperConfigFile="$buildAgentFolder\launcher\conf\wrapper.conf"
$binFolder="$buildAgentFolder\bin"

Write-Host "`nStopping TeamCity build agent service..."
cd $binFolder
& .\service.stop.bat
& .\service.uninstall.bat

Write-Host "`nCopying SSH keys to $env:USERPROFILE..."
copy "$setupFilesShare\.ssh" "$env:USERPROFILE\.ssh" -Recurse -Force

Write-Host "`nChanging TeamCity service account password in file '$wrapperConfigFile'..."
(Get-Content $wrapperConfigFile) | ForEach-Object { 
    $_ -replace '#ACCOUNT_NAME#', $teamAgentServiceAccountName `
       -replace '#ACCOUNT_PASSWORD#', $teamAgentServiceAccountPassword `
} | Set-Content $wrapperConfigFile

Write-Host "`nStarting TeamCity build agent service..."
& .\service.install.bat
& .\service.start.bat