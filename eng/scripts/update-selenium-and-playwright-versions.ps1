# Define the packages and their corresponding XML entries
$packages = @{
    "Microsoft.Playwright" = "MicrosoftPlaywrightVersion"
    "Selenium.Support" = "SeleniumSupportVersion"
    "Selenium.WebDriver" = "SeleniumWebDriverVersion"
  }

  # Function to get the latest stable version from NuGet
function Get-LatestNuGetVersion {
    param (
        [string]$packageName
    )
    $packageName = $packageName.ToLower();
    $url = "https://api.nuget.org/v3-flatcontainer/$packageName/index.json"
    $response = Invoke-RestMethod -Uri $url
    $versions = $response.versions | Where-Object { $_ -notmatch "-" } | ForEach-Object { [System.Version]$_ }
    return ($versions | Sort-Object -Property Major, Minor, Build, Revision -Descending | Select-Object -First 1).ToString()
}

# Function to update the Versions.props file
function Update-VersionsProps {
param (
    [string]$filePath,
    [hashtable]$versions
)
$content = Get-Content -Path $filePath
foreach ($package in $versions.Keys) {
    $entryName = $packages[$package]
    $version = $versions[$package]
    $pattern = "(?<=<$entryName>)(.*?)(?=</$entryName>)"
    $replacement = $version
    $content = $content -replace $pattern, $replacement
}
Set-Content -Path $filePath -Value $content
}

# Function to check if the Docker image exists
function Test-DockerImageExists {
param (
    [string]$imageName,
    [string]$version
)
$url = "https://mcr.microsoft.com/v2/$imageName/tags/list"
$response = Invoke-RestMethod -Uri $url
return $response.tags -contains $version
}

# Function to update the Dockerfile
function Update-Dockerfile {
param (
    [string]$filePath,
    [string]$version
)
(Get-Content -Path $filePath) -replace 'FROM mcr.microsoft.com/playwright/dotnet:.* AS final', "FROM mcr.microsoft.com/playwright/dotnet:$version AS final" | Set-Content -Path $filePath
}

# Get the latest versions of the packages
$versions = @{}
foreach ($package in $packages.Keys) {
$versions[$package] = Get-LatestNuGetVersion -packageName $package
}

# Print the package versions found
foreach ($package in $versions.Keys) {
    Write-Host "$($package): $($versions[$package])"
}

# Update the Versions.props file
Update-VersionsProps -filePath "eng/Versions.props" -versions $versions

# Check if the Docker image exists
$playwrightVersion = "v$($versions["Microsoft.Playwright"])-jammy-amd64"
if (Test-DockerImageExists -imageName "playwright/dotnet" -version $playwrightVersion) {
# Update the Dockerfile
Update-Dockerfile -filePath "src/Components/benchmarkapps/Wasm.Performance/dockerfile" -version $playwrightVersion
} else {
Write-Error "Docker image for Playwright version $playwrightVersion not found."
exit 1
}

# Check if there are changes
git diff --quiet
if ($LASTEXITCODE -eq 0) {
    Write-Host "No changes to commit."
    # exit 0
}else {
    Write-Host "Updating the version of packages";
}

# Create a new branch
git checkout -b infrastructure/update-selenium-and-typescript-dependencies

# Stage the changes
git add eng/Versions.props src/Components/benchmarkapps/Wasm.Performance/dockerfile

# Commit the changes
$commitMessage = @"
[Infrastructure] Update Selenium and Playwright dependencies $(Get-Date -Format "yyyy-MM-dd")
* Updated Microsoft.Playwright version to $($versions["Microsoft.Playwright"])
* Updated Selenium.Support version to $($versions["Selenium.Support"])
* Updated Selenium.WebDriver version to $($versions["Selenium.WebDriver"])
"@
git commit -m $commitMessage

# Push the branch
git push origin infrastructure/update-selenium-and-typescript-dependencies

$prBody = $commitMessage + @"

Please see the [MirroringPackages.md](https://github.com/dotnet/arcade/blob/main/Documentation/MirroringPackages.md) document in the [dotnet/arcade](https://github.com/dotnet/arcade) repository for information on how to mirror these packages on the MS NuGet feed.
"@

gh pr create --title "Update Selenium and Playwright dependencies $(Get-Date -Format "yyyy-MM-dd")" --body $prBody --base main --head infrastructure/update-selenium-and-typescript-dependencies
