
function Install-Gdn {
    param(
        [Parameter(Mandatory=$true)]
        [string]$Path,

        [string]$Source = "https://pkgs.dev.azure.com/dnceng/_packaging/Guardian1ESPTUpstreamOrgFeed/nuget/v3/index.json",

        # If omitted, install the latest version of Guardian, otherwise install that specific version.
        [string]$Version
    )

    $ErrorActionPreference = 'Stop'
    Set-StrictMode -Version 2.0
    $disableConfigureToolsetImport = $true
    $global:LASTEXITCODE = 0

    # `tools.ps1` checks $ci to perform some actions. Since the SDL
    # scripts don't necessarily execute in the same agent that run the
    # build.ps1/sh script this variable isn't automatically set.
    $ci = $true
    . $PSScriptRoot\..\tools.ps1

    $argumentList = @("install", "Microsoft.Guardian.Cli.win-x64", "-Source $Source", "-OutputDirectory $Path", "-NonInteractive", "-NoCache")

    if ($Version) {
        $argumentList += "-Version $Version"
    }
    
    Start-Process nuget -Verbose -ArgumentList $argumentList -NoNewWindow -Wait

    $gdnCliPath = Get-ChildItem -Filter guardian.cmd -Recurse -Path $Path

    if (!$gdnCliPath)
    {
        Write-PipelineTelemetryError -Category 'Sdl' -Message 'Failure installing Guardian'
    }

    return $gdnCliPath.FullName
}