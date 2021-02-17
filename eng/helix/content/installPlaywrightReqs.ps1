Write-Host "Set-ExecutionPolicy Bypass -Scope Process"
Set-ExecutionPolicy Bypass -Scope Process
if ((Get-WindowsOptionalFeature -FeatureName ServerMediaFoundation -Online).State -eq "Enabled") {
    Write-Host "ServerMediaFoundation feature already enabled."
} else {
    Write-Host "Enable-WindowsOptionalFeature -Online -FeatureName ServerMediaFoundation -Confirm:$false (For Playwright)"
    try {
        Enable-WindowsOptionalFeature -Online -FeatureName ServerMediaFoundation -Confirm:$false
    } catch {
        Write-Host "Enable-WindowsOptionalFeature -Online -FeatureName ServerMediaFoundation -Confirm:$false threw an exception: $PSItem"
    }
}