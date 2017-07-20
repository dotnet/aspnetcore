$infoOutput = dotnet --info
$versions = $infoOutput | Select-String -Pattern "version"
$FXVersionRaw = $versions | Select-Object -Last 1
$FXVersionString = $FXVersionRaw.ToString()
$FXVersion = $FXVersionString.SubString($FXVersionString.IndexOf(':') + 1).Trim()
Write-Host $FXVersion
