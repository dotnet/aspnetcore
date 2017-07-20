param (
    [string]$sharedRuntimeVersion,
    [string]$installationDirectory
)

$sharedRuntimeChannel = "master"
if ($env:KOREBUILD_DOTNET_SHARED_RUNTIME_CHANNEL)
{
    $sharedRuntimeChannel = $env:KOREBUILD_DOTNET_SHARED_RUNTIME_CHANNEL
}

function InstallSharedRuntime([string] $version, [string] $channel, [string] $installDir)
{
    $sharedRuntimePath = [IO.Path]::Combine($installDir, 'shared', 'Microsoft.NETCore.App', $version)
    # Avoid redownloading the CLI if it's already installed.
    if (!(Test-Path $sharedRuntimePath))
    {
        & "$PSScriptRoot\..\.build\dotnet\dotnet-install.ps1" `
            -Channel $channel `
            -SharedRuntime `
            -Version $version `
            -Architecture 'x86' `
            -InstallDir $installDir `
            -NoPath
    }
}

InstallSharedRuntime -version $sharedRuntimeVersion -channel $sharedRuntimeChannel -installDir $installationDirectory
