param(
    [string]$Target,
    [string]$SdkVersion,
    [string]$RuntimeVersion,
    [string]$AspRuntimeVersion,
    [string]$Queue,
    [string]$Arch,
    [string]$Quarantined,
    [string]$EF,
    [string]$HelixTimeout,
    [string]$InstallPlaywright,
    [string]$FeedCred
)

$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = 1
$env:DOTNET_MULTILEVEL_LOOKUP = 0
$env:InstallPlaywright = "$InstallPlaywright"
$currentDirectory = Get-Location
$env:PLAYWRIGHT_BROWSERS_PATH = "$currentDirectory\ms-playwright"
$env:PLAYWRIGHT_DRIVER_PATH = "$currentDirectory\.playwright\win-x64\native\playwright.cmd"

$envPath = "$env:PATH;$env:HELIX_CORRELATION_PAYLOAD\node\bin"

Write-Host "Restore: dotnet restore RunTests\RunTests.csproj --ignore-failed-sources"
dotnet restore RunTests\RunTests.csproj --ignore-failed-sources

if ($LastExitCode -ne 0) {
    exit $LastExitCode
}

Write-Host "Running tests: dotnet run --no-restore --project RunTests\RunTests.csproj -- --target $Target --runtime $AspRuntimeVersion --queue $Queue --arch $Arch --quarantined $Quarantined --ef $EF --helixTimeout $HelixTimeout"
dotnet run --no-restore --project RunTests\RunTests.csproj -- --target $Target --runtime $AspRuntimeVersion --queue $Queue --arch $Arch --quarantined $Quarantined --ef $EF --helixTimeout $HelixTimeout

Write-Host "Finished running tests: exit_code=$LastExitCode"
exit $LastExitCode
