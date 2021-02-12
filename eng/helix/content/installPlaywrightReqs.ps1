Write-Host "Enable-WindowsOptionalFeature -Online -FeatureName ServerMediaFoundation  (For Playwright)"
try {
    Enable-WindowsOptionalFeature -Online -FeatureName ServerMediaFoundation 
} catch {
    Write-Host "Enable-WindowsOptionalFeature -Online -FeatureName ServerMediaFoundation threw an exception: $PSItem.Exception.Message"
}
