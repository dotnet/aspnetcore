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

$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = 1
$env:DOTNET_MULTILEVEL_LOOKUP = 0

$currentDirectory = Get-Location
$envPath = "$env:PATH;$env:HELIX_CORRELATION_PAYLOAD\node\bin"

function InvokeInstallDotnet([string]$command) {
    Write-Host "InstallDotNet $command"

    $timeoutSeconds = 180
    $proc = Start-Process -filePath powershell.exe -ArgumentList "-noLogo -NoProfile -ExecutionPolicy unrestricted -command `"$command`"" -NoNewWindow -PassThru

    $proc | Wait-Process -Timeout $timeoutSeconds
    $exitCode = $proc.ExitCode

    if ($exitCode -eq 0) {
        Write-Host "InstallDotNet $command completed successfully"
        return $true
    }
    elseif ([string]::IsNullOrWhiteSpace($exitCode)) {
        Write-Warning "InstallDotNet $command timed out after $timeoutSeconds seconds"
    }
    else {
        Write-Warning "InstallDotNet $command failed with exit code $exitCode"
    }

    $proc | Stop-Process -Force

    return $false
}

function InstallDotnetSDKAndRuntime([string]$Feed, [string]$FeedCredParam) {
    foreach ($i in 1..5) {
        $random = Get-Random -Maximum 1024
        $env:DOTNET_HOME = Join-Path $currentDirectory "sdk$random"
        $env:DOTNET_ROOT = Join-Path $env:DOTNET_HOME $Arch
        $env:DOTNET_CLI_HOME = Join-Path $currentDirectory "home$random"
        $env:PATH = "$env:DOTNET_ROOT;$envPath"

        Write-Host "Set PATH to: $env:PATH"

        $success = InvokeInstallDotnet ". eng\common\tools.ps1; InstallDotNet $env:DOTNET_ROOT $SdkVersion $Arch `'`' `$true `'$Feed`' `'$FeedCredParam`' `$true"

        if (!$success) {
            Write-Host "Retrying..."
            continue
        }

        $success = InvokeInstallDotnet ". eng\common\tools.ps1; InstallDotNet $env:DOTNET_ROOT $RuntimeVersion $Arch dotnet `$true `'$Feed`' `'$FeedCredParam`' `$true"

        if (!$success) {
            Write-Host "Retrying..."
            continue
        }

        return
    }

    Write-Error "InstallDotNet $command exceeded retry limit"
    exit 1
}

if ([string]::IsNullOrEmpty($FeedCred)) {
    InstallDotnetSDKAndRuntime
} else {
    InstallDotnetSDKAndRuntime "https://dotnetclimsrc.blob.core.windows.net/dotnet" $FeedCred
}

Write-Host "Install-WindowsFeature Server-Media-Foundation (For Playwright)"
Install-WindowsFeature Server-Media-Foundation

Write-Host "Restore: dotnet restore RunTests\RunTests.csproj --ignore-failed-sources"
dotnet restore RunTests\RunTests.csproj --ignore-failed-sources

if ($LastExitCode -ne 0) {
    exit $LastExitCode
}

Write-Host "Running tests: dotnet run --no-restore --project RunTests\RunTests.csproj -- --target $Target --runtime $AspRuntimeVersion --queue $Queue --arch $Arch --quarantined $Quarantined --ef $EF --helixTimeout $HelixTimeout"
dotnet run --no-restore --project RunTests\RunTests.csproj -- --target $Target --runtime $AspRuntimeVersion --queue $Queue --arch $Arch --quarantined $Quarantined --ef $EF --helixTimeout $HelixTimeout

Write-Host "Finished running tests: exit_code=$LastExitCode"
exit $LastExitCode
