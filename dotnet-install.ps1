#
# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
#

<#
.SYNOPSIS
    Installs dotnet cli
.DESCRIPTION
    Installs dotnet cli. If dotnet installation already exists in the given directory
    it will update it only if the requested version differs from the one already installed.
.PARAMETER Channel
    Default: preview
    Channel is the way of reasoning about stability and quality of dotnet. This parameter takes one of the values:
    - future - Possibly unstable, frequently changing, may contain new finished and unfinished features
    - preview - Pre-release stable with known issues and feature gaps
    - production - Most stable releases
.PARAMETER Version
    Default: latest
    Represents a build version on specific channel. Possible values:
    - 4-part version in a format A.B.C.D - represents specific version of build
    - latest - most latest build on specific channel
    - lkg - last known good version on specific channel
    Note: LKG work is in progress. Once the work is finished, this will become new default
.PARAMETER InstallDir
    Default: %LocalAppData%\Microsoft\dotnet
    Path to where to install dotnet. Note that binaries will be placed directly in a given directory.
.PARAMETER Architecture
    Default: <auto> - this value represents currently running OS architecture
    Architecture of dotnet binaries to be installed.
    Possible values are: <auto>, x64 and x86
.PARAMETER SharedRuntime
    Default: false
    Installs just the shared runtime bits, not the entire SDK
.PARAMETER DebugSymbols
    If set the installer will include symbols in the installation.
.PARAMETER DryRun
    If set it will not perform installation but instead display what command line to use to consistently install
    currently requested version of dotnet cli. In example if you specify version 'latest' it will display a link
    with specific version so that this command can be used deterministicly in a build script.
    It also displays binaries location if you prefer to install or download it yourself.
.PARAMETER NoPath
    By default this script will set environment variable PATH for the current process to the binaries folder inside installation folder.
    If set it will display binaries location but not set any environment variable.
.PARAMETER Verbose
    Displays diagnostics information.
.PARAMETER AzureFeed
    Default: https://dotnetcli.azureedge.net/dotnet
    This parameter should not be usually changed by user. It allows to change URL for the Azure feed used by this installer.
.PARAMETER ProxyAddress
    If set, the installer will use the proxy when making web requests
#>
[cmdletbinding()]
param(
   [string]$Channel="rel-1.0.0",
   [string]$Version="Latest",
   [string]$InstallDir="<auto>",
   [string]$Architecture="<auto>",
   [switch]$SharedRuntime,
   [switch]$DebugSymbols, # TODO: Switch does not work yet. Symbols zip is not being uploaded yet.
   [switch]$DryRun,
   [switch]$NoPath,
   [string]$AzureFeed="https://dotnetcli.azureedge.net/dotnet",
   [string]$UncachedFeed="https://dotnetcli.blob.core.windows.net/dotnet",
   [string]$ProxyAddress
)

Set-StrictMode -Version Latest
$ErrorActionPreference="Stop"
$ProgressPreference="SilentlyContinue"

$BinFolderRelativePath=""

# example path with regex: shared/1.0.0-beta-12345/somepath
$VersionRegEx="/\d+\.\d+[^/]+/"
$OverrideNonVersionedFiles=$true

function Say($str) {
    Write-Host "dotnet-install: $str"
}

function Say-Verbose($str) {
    Write-Verbose "dotnet-install: $str"
}

function Say-Invocation($Invocation) {
    $command = $Invocation.MyCommand;
    $args = (($Invocation.BoundParameters.Keys | foreach { "-$_ `"$($Invocation.BoundParameters[$_])`"" }) -join " ")
    Say-Verbose "$command $args"
}

function Get-Machine-Architecture() {
    Say-Invocation $MyInvocation

    # possible values: AMD64, IA64, x86
    return $ENV:PROCESSOR_ARCHITECTURE
}

# TODO: Architecture and CLIArchitecture should be unified
function Get-CLIArchitecture-From-Architecture([string]$Architecture) {
    Say-Invocation $MyInvocation

    switch ($Architecture.ToLower()) {
        { $_ -eq "<auto>" } { return Get-CLIArchitecture-From-Architecture $(Get-Machine-Architecture) }
        { ($_ -eq "amd64") -or ($_ -eq "x64") } { return "x64" }
        { $_ -eq "x86" } { return "x86" }
        default { throw "Architecture not supported. If you think this is a bug, please report it at https://github.com/dotnet/cli/issues" }
    }
}

function Get-Version-Info-From-Version-Text([string]$VersionText) {
    Say-Invocation $MyInvocation

    $Data = @($VersionText.Split([char[]]@(), [StringSplitOptions]::RemoveEmptyEntries));

    $VersionInfo = @{}
    $VersionInfo.CommitHash = $Data[0].Trim()
    $VersionInfo.Version = $Data[1].Trim()
    return $VersionInfo
}

function Load-Assembly([string] $Assembly) {
    try {
        Add-Type -Assembly $Assembly | Out-Null
    }
    catch {
        # On Nano Server, Powershell Core Edition is used.  Add-Type is unable to resolve base class assemblies because they are not GAC'd.
        # Loading the base class assemblies is not unnecessary as the types will automatically get resolved.
    }
}

function GetHTTPResponse([Uri] $Uri)
{
    $HttpClient = $null

    try {
        # HttpClient is used vs Invoke-WebRequest in order to support Nano Server which doesn't support the Invoke-WebRequest cmdlet.
        Load-Assembly -Assembly System.Net.Http
        if($ProxyAddress){
            $HttpClientHandler = New-Object System.Net.Http.HttpClientHandler
            $HttpClientHandler.Proxy =  New-Object System.Net.WebProxy -Property @{Address=$ProxyAddress}
            $HttpClient = New-Object System.Net.Http.HttpClient -ArgumentList $HttpClientHandler
        } 
        else {
            $HttpClient = New-Object System.Net.Http.HttpClient
        }

        $Response = $HttpClient.GetAsync($Uri).Result
        if (($Response -eq $null) -or (-not ($Response.IsSuccessStatusCode)))
        {
            $ErrorMsg = "Failed to download $Uri."
            if ($Response -ne $null)
            {
                $ErrorMsg += "  $Response"
            }

            throw $ErrorMsg
        }

        return $Response
    }
    finally {
        if ($HttpClient -ne $null) {
            $HttpClient.Dispose()
        }
    }
}


function Get-Latest-Version-Info([string]$AzureFeed, [string]$AzureChannel, [string]$CLIArchitecture) {
    Say-Invocation $MyInvocation

    $VersionFileUrl = $null
    if ($SharedRuntime) {
        $VersionFileUrl = "$UncachedFeed/$AzureChannel/dnvm/latest.sharedfx.win.$CLIArchitecture.version"
    }
    else {
        $VersionFileUrl = "$UncachedFeed/Sdk/$AzureChannel/latest.version"
    }
    
    $Response = GetHTTPResponse -Uri $VersionFileUrl
    $StringContent = $Response.Content.ReadAsStringAsync().Result

    switch ($Response.Content.Headers.ContentType) {
        { ($_ -eq "application/octet-stream") } { $VersionText = [Text.Encoding]::UTF8.GetString($StringContent) }
        { ($_ -eq "text/plain") } { $VersionText = $StringContent }
        default { throw "``$Response.Content.Headers.ContentType`` is an unknown .version file content type." }
    }

    $VersionInfo = Get-Version-Info-From-Version-Text $VersionText

    return $VersionInfo
}

# TODO: AzureChannel and Channel should be unified
function Get-Azure-Channel-From-Channel([string]$Channel) {
    Say-Invocation $MyInvocation

    # For compatibility with build scripts accept also directly Azure channels names
    switch ($Channel.ToLower()) {
        { ($_ -eq "future") -or ($_ -eq "dev") } { return "dev" }
        { $_ -eq "production" } { throw "Production channel does not exist yet" }
        default { return $_ }
    }
}

function Get-Specific-Version-From-Version([string]$AzureFeed, [string]$AzureChannel, [string]$CLIArchitecture, [string]$Version) {
    Say-Invocation $MyInvocation

    switch ($Version.ToLower()) {
        { $_ -eq "latest" } {
            $LatestVersionInfo = Get-Latest-Version-Info -AzureFeed $AzureFeed -AzureChannel $AzureChannel -CLIArchitecture $CLIArchitecture
            return $LatestVersionInfo.Version
        }
        { $_ -eq "lkg" } { throw "``-Version LKG`` not supported yet." }
        default { return $Version }
    }
}

function Get-Download-Links([string]$AzureFeed, [string]$AzureChannel, [string]$SpecificVersion, [string]$CLIArchitecture) {
    Say-Invocation $MyInvocation
    
    $ret = @()
    
    if ($SharedRuntime) {
        $PayloadURL = "$AzureFeed/$AzureChannel/Binaries/$SpecificVersion/dotnet-win-$CLIArchitecture.$SpecificVersion.zip"
    }
    else {
        $PayloadURL = "$AzureFeed/Sdk/$SpecificVersion/dotnet-dev-win-$CLIArchitecture.$SpecificVersion.zip"
    }

    Say-Verbose "Constructed payload URL: $PayloadURL"
    $ret += $PayloadURL

    return $ret
}

function Get-User-Share-Path() {
    Say-Invocation $MyInvocation

    $InstallRoot = $env:DOTNET_INSTALL_DIR
    if (!$InstallRoot) {
        $InstallRoot = "$env:LocalAppData\Microsoft\dotnet"
    }
    return $InstallRoot
}

function Resolve-Installation-Path([string]$InstallDir) {
    Say-Invocation $MyInvocation

    if ($InstallDir -eq "<auto>") {
        return Get-User-Share-Path
    }
    return $InstallDir
}

function Get-Version-Info-From-Version-File([string]$InstallRoot, [string]$RelativePathToVersionFile) {
    Say-Invocation $MyInvocation

    $VersionFile = Join-Path -Path $InstallRoot -ChildPath $RelativePathToVersionFile
    Say-Verbose "Local version file: $VersionFile"
    
    if (Test-Path $VersionFile) {
        $VersionText = cat $VersionFile
        Say-Verbose "Local version file text: $VersionText"
        return Get-Version-Info-From-Version-Text $VersionText
    }

    Say-Verbose "Local version file not found."

    return $null
}

function Is-Dotnet-Package-Installed([string]$InstallRoot, [string]$RelativePathToPackage, [string]$SpecificVersion) {
    Say-Invocation $MyInvocation
    
    $DotnetPackagePath = Join-Path -Path $InstallRoot -ChildPath $RelativePathToPackage | Join-Path -ChildPath $SpecificVersion
    Say-Verbose "Is-Dotnet-Package-Installed: Path to a package: $DotnetPackagePath"
    return Test-Path $DotnetPackagePath -PathType Container
}

function Get-Absolute-Path([string]$RelativeOrAbsolutePath) {
    # Too much spam
    # Say-Invocation $MyInvocation

    return $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($RelativeOrAbsolutePath)
}

function Get-Path-Prefix-With-Version($path) {
    $match = [regex]::match($path, $VersionRegEx)
    if ($match.Success) {
        return $entry.FullName.Substring(0, $match.Index + $match.Length)
    }
    
    return $null
}

function Get-List-Of-Directories-And-Versions-To-Unpack-From-Dotnet-Package([System.IO.Compression.ZipArchive]$Zip, [string]$OutPath) {
    Say-Invocation $MyInvocation
    
    $ret = @()
    foreach ($entry in $Zip.Entries) {
        $dir = Get-Path-Prefix-With-Version $entry.FullName
        if ($dir -ne $null) {
            $path = Get-Absolute-Path $(Join-Path -Path $OutPath -ChildPath $dir)
            if (-Not (Test-Path $path -PathType Container)) {
                $ret += $dir
            }
        }
    }
    
    $ret = $ret | Sort-Object | Get-Unique
    
    $values = ($ret | foreach { "$_" }) -join ";"
    Say-Verbose "Directories to unpack: $values"
    
    return $ret
}

# Example zip content and extraction algorithm:
# Rule: files if extracted are always being extracted to the same relative path locally
# .\
#       a.exe   # file does not exist locally, extract
#       b.dll   # file exists locally, override only if $OverrideFiles set
#       aaa\    # same rules as for files
#           ...
#       abc\1.0.0\  # directory contains version and exists locally
#           ...     # do not extract content under versioned part
#       abc\asd\    # same rules as for files
#            ...
#       def\ghi\1.0.1\  # directory contains version and does not exist locally
#           ...         # extract content
function Extract-Dotnet-Package([string]$ZipPath, [string]$OutPath) {
    Say-Invocation $MyInvocation

    Load-Assembly -Assembly System.IO.Compression.FileSystem
    Set-Variable -Name Zip
    try {
        $Zip = [System.IO.Compression.ZipFile]::OpenRead($ZipPath)
        
        $DirectoriesToUnpack = Get-List-Of-Directories-And-Versions-To-Unpack-From-Dotnet-Package -Zip $Zip -OutPath $OutPath
        
        foreach ($entry in $Zip.Entries) {
            $PathWithVersion = Get-Path-Prefix-With-Version $entry.FullName
            if (($PathWithVersion -eq $null) -Or ($DirectoriesToUnpack -contains $PathWithVersion)) {
                $DestinationPath = Get-Absolute-Path $(Join-Path -Path $OutPath -ChildPath $entry.FullName)
                $DestinationDir = Split-Path -Parent $DestinationPath
                $OverrideFiles=$OverrideNonVersionedFiles -Or (-Not (Test-Path $DestinationPath))
                if ((-Not $DestinationPath.EndsWith("\")) -And $OverrideFiles) {
                    New-Item -ItemType Directory -Force -Path $DestinationDir | Out-Null
                    [System.IO.Compression.ZipFileExtensions]::ExtractToFile($entry, $DestinationPath, $OverrideNonVersionedFiles)
                }
            }
        }
    }
    finally {
        if ($Zip -ne $null) {
            $Zip.Dispose()
        }
    }
}

function DownloadFile([Uri]$Uri, [string]$OutPath) {
    $Stream = $null

    try {
        $Response = GetHTTPResponse -Uri $Uri
        $Stream = $Response.Content.ReadAsStreamAsync().Result
        $File = [System.IO.File]::Create($OutPath)
        $Stream.CopyTo($File)
        $File.Close()
    }
    finally {
        if ($Stream -ne $null) {
            $Stream.Dispose()
        }
    }
}

$AzureChannel = Get-Azure-Channel-From-Channel -Channel $Channel
$CLIArchitecture = Get-CLIArchitecture-From-Architecture $Architecture
$SpecificVersion = Get-Specific-Version-From-Version -AzureFeed $AzureFeed -AzureChannel $AzureChannel -CLIArchitecture $CLIArchitecture -Version $Version
$DownloadLinks = Get-Download-Links -AzureFeed $AzureFeed -AzureChannel $AzureChannel -SpecificVersion $SpecificVersion -CLIArchitecture $CLIArchitecture

if ($DryRun) {
    Say "Payload URLs:"
    foreach ($DownloadLink in $DownloadLinks) {
        Say "- $DownloadLink"
    }
    Say "Repeatable invocation: .\$($MyInvocation.MyCommand) -Version $SpecificVersion -Channel $Channel -Architecture $CLIArchitecture -InstallDir $InstallDir"
    exit 0
}

$InstallRoot = Resolve-Installation-Path $InstallDir
Say-Verbose "InstallRoot: $InstallRoot"

$IsSdkInstalled = Is-Dotnet-Package-Installed -InstallRoot $InstallRoot -RelativePathToPackage "sdk" -SpecificVersion $SpecificVersion
Say-Verbose ".NET SDK installed? $IsSdkInstalled"
if ($IsSdkInstalled) {
    Say ".NET SDK version $SpecificVersion is already installed."
    exit 0
}

New-Item -ItemType Directory -Force -Path $InstallRoot | Out-Null

foreach ($DownloadLink in $DownloadLinks) {
    $ZipPath = [System.IO.Path]::GetTempFileName()
    Say "Downloading $DownloadLink"
    DownloadFile -Uri $DownloadLink -OutPath $ZipPath

    Say "Extracting zip from $DownloadLink"
    Extract-Dotnet-Package -ZipPath $ZipPath -OutPath $InstallRoot

    Remove-Item $ZipPath
}

$BinPath = Get-Absolute-Path $(Join-Path -Path $InstallRoot -ChildPath $BinFolderRelativePath)
if (-Not $NoPath) {
    Say "Adding to current process PATH: `"$BinPath`". Note: This change will not be visible if PowerShell was run as a child process."
    $env:path = "$BinPath;" + $env:path
}
else {
    Say "Binaries of dotnet can be found in $BinPath"
}

Say "Installation finished"
exit 0
