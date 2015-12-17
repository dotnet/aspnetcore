#
# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
#

$ErrorActionPreference="Stop"
$ProgressPreference="SilentlyContinue"

$Feed="https://dotnetcli.blob.core.windows.net/dotnet"
$Channel="dev"
$DotNetFileName="dotnet-win-x64.latest.zip"
$DotNetUrl="$Feed/$Channel/Binaries/Latest"

function say($str)
{
    Write-Host "dotnet_install: $str"
}

$InstallDir = $env:DOTNET_INSTALL_DIR
if (!$InstallDir) {
    $InstallDir = "$env:LocalAppData\Microsoft\dotnet"
}

say "Preparing to install .NET Tools to $InstallDir"

# Check if we need to bother
$LocalFile = "$InstallDir\cli\.version"
if (Test-Path $LocalFile)
{
    $LocalData = @(cat $LocalFile)
    $LocalHash = $LocalData[0].Trim()
    $LocalVersion = $LocalData[1].Trim()
    if ($LocalVersion -and $LocalHash)
    {
        $RemoteResponse = Invoke-WebRequest -UseBasicParsing "$Feed/$Channel/dnvm/latest.win.version"
        $RemoteData = @([Text.Encoding]::UTF8.GetString($RemoteResponse.Content).Split([char[]]@(), [StringSplitOptions]::RemoveEmptyEntries));
        $RemoteHash = $RemoteData[0].Trim()
        $RemoteVersion = $RemoteData[1].Trim()

        if (!$RemoteVersion -or !$RemoteHash) {
            throw "Invalid response from feed"
        }

        say "Latest version: $RemoteVersion"
        say "Local Version: $LocalVersion"

        if($LocalHash -eq $RemoteHash)
        {
            say "You already have the latest version"
            exit 0
        }
    }
}

# Set up the install location
if (!(Test-Path $InstallDir)) {
    mkdir $InstallDir | Out-Null
}

# De-powershell the path before passing to .NET APIs
$InstallDir = Convert-Path $InstallDir

say "Downloading $DotNetFileName from $DotNetUrl"
$resp = Invoke-WebRequest -UseBasicParsing "$DotNetUrl/$DotNetFileName" -OutFile "$InstallDir\$DotNetFileName"

say "Extracting zip"

# Create the destination
if (Test-Path "$InstallDir\cli_new") {
    del -rec -for "$InstallDir\cli_new"
}
mkdir "$InstallDir\cli_new" | Out-Null

Add-Type -Assembly System.IO.Compression.FileSystem | Out-Null
[System.IO.Compression.ZipFile]::ExtractToDirectory("$InstallDir\$DotNetFileName", "$InstallDir\cli_new")

# Replace the old installation (if any)
if (Test-Path "$InstallDir\cli") {
    del -rec -for "$InstallDir\cli"
}
mv "$InstallDir\cli_new" "$InstallDir\cli"

# Clean the zip
if (Test-Path "$InstallDir\$DotNetFileName") {
    del -for "$InstallDir\$DotNetFileName"
}

say "The .NET Tools have been installed to $InstallDir\cli!"

# New layout
say "Add '$InstallDir\cli\bin' to your PATH to use dotnet"
