param($RootDirectory = (Get-Location), $Framework = "net9.0", $Runtime = "win-x64", $CommitHash, $BranchName, $BuildNumber)

# De-Powershell the path
$RootDirectory = (Convert-Path $RootDirectory)

# Find dotnet.exe
$dotnet = Join-Path (Join-Path $env:USERPROFILE ".dotnet") "dotnet.exe"

if(!(Test-Path $dotnet)) {
    throw "Could not find dotnet at: $dotnet"
}

# Resolve directories
$SamplesDir = Join-Path $RootDirectory "samples"
$ArtifactsDir = Join-Path $RootDirectory "artifacts"
$AppsDir = Join-Path $ArtifactsDir "apps"
$ClientsDir = Join-Path $RootDirectory "clients"
$ClientsTsDir = Join-Path $ClientsDir "ts"

# The list of apps to publish
$Apps = @{
    "SignalRSamples"= (Join-Path $SamplesDir "SignalRSamples")
    "FunctionalTests"= (Join-Path $ClientsTsDir "FunctionalTests/SignalR.Client.FunctionalTestApp.csproj")
}

$BuildMetadataContent = @"
[assembly: System.Reflection.AssemblyMetadata("CommitHash", "$($CommitHash)")]
[assembly: System.Reflection.AssemblyMetadata("BranchName", "$($BranchName)")]
[assembly: System.Reflection.AssemblyMetadata("BuildNumber", "$($BuildNumber)")]
[assembly: System.Reflection.AssemblyMetadata("BuildDateUtc", "$([DateTime]::UtcNow.ToString("O"))")]
"@

$Apps.Keys | ForEach-Object {
    $Name = $_
    $Path = $Apps[$_]

    $OutputDir = Join-Path $AppsDir $Name

    # Hacky but it works for now
    $MetadataPath = Join-Path $Path "BuildMetadata.cs"
    $BuildMetadataContent > $MetadataPath
    try {
        Write-Host -ForegroundColor Green "Publishing $Name"
        & "$dotnet" publish --framework $Framework --runtime $Runtime --output $OutputDir $Path
    } finally {
        Remove-Item $MetadataPath
    }
}
