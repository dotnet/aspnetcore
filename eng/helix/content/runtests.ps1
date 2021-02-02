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
    [string]$FeedCred
)

$currentDirectory = Get-Location
$random = Get-Random -Maximum 1024

$env:DOTNET_HOME = Join-Path $currentDirectory "sdk$random"
$env:DOTNET_ROOT = Join-Path $env:DOTNET_HOME $Arch
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = 1
$env:DOTNET_MULTILEVEL_LOOKUP = 0
$env:DOTNET_CLI_HOME = Join-Path $currentDirectory "home$random"
$env:PATH = "$env:DOTNET_ROOT;$env:PATH;$env:HELIX_CORRELATION_PAYLOAD\node\bin"

Write-Host "Set PATH to: $env:PATH"

function InvokeInstallDotnet([string]$command) {
    foreach ($i in 1..5) {
        Write-Host "InstallDotNet $command"

        $timeoutSeconds = 10
        $proc = Start-Process -filePath powershell.exe -ArgumentList "-noLogo -NoProfile -ExecutionPolicy unrestricted -command `"$command`"" -NoNewWindow -PassThru

        $proc | Wait-Process -Timeout $timeoutSeconds
        $exitCode = $proc.ExitCode

        if ($exitCode -eq 0) {
            Write-Host "InstallDotNet $command completed successfully"
            return
        }

        if ([string]::IsNullOrWhiteSpace($exitCode)) {
            Write-Warning "InstallDotNet $command timed out after $timeoutSeconds seconds retrying..."
        }
        else {
            Write-Warning "InstallDotNet $command failed with exit code $exitCode retrying..."
        }

        $proc | Stop-Process -Force
    }

    Write-Warning "InstallDotNet $command exceeded retry limit"
    exit 1
}

if ($FeedCred -eq $null) (
    InvokeInstallDotnet(". eng\common\tools.ps1; InstallDotNet $env:DOTNET_ROOT $SdkVersion $Arch `'`' `$true `'`' `'`' `$true")
    InvokeInstallDotnet(". eng\common\tools.ps1; InstallDotNet $env:DOTNET_ROOT $RuntimeVersion $Arch dotnet `$true `'`' `'`' `$true")
) else (
    InvokeInstallDotnet(". eng\common\tools.ps1; InstallDotNet $env:DOTNET_ROOT $SdkVersion $Arch `'`' `$true https://dotnetclimsrc.blob.core.windows.net/dotnet $FeedCred `$true")
    InvokeInstallDotnet(". eng\common\tools.ps1; InstallDotNet $env:DOTNET_ROOT $RuntimeVersion $Arch dotnet `$true https://dotnetclimsrc.blob.core.windows.net/dotnet $FeedCred `$true")
)

Write-Host "Restore: dotnet restore RunTests\RunTests.csproj --ignore-failed-sources"
dotnet restore RunTests\RunTests.csproj --ignore-failed-sources

if ($LastExitCode -ne 0) {
    exit $LastExitCode
}

Write-Host "Running tests: dotnet run --no-restore --project RunTests\RunTests.csproj -- --target $Target --runtime $AspRuntimeVersion --queue $Queue --arch $Arch --quarantined $Quarantined --ef $EF --helixTimeout $HelixTimeout"
dotnet run --no-restore --project RunTests\RunTests.csproj -- --target $Target --runtime $AspRuntimeVersion --queue $Queue --arch $Arch --quarantined $Quarantined --ef $EF --helixTimeout $HelixTimeout

Write-Host "Finished running tests: exit_code=$LastExitCode"
exit $LastExitCode"