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
    Default: LTS
    Download from the Channel specified. Possible values:
    - Current - most current release
    - LTS - most current supported release
    - 2-part version in a format A.B - represents a specific release
          examples: 2.0, 1.0
    - Branch name
          examples: release/2.0.0, Master
    Note: The version parameter overrides the channel parameter.
.PARAMETER Version
    Default: latest
    Represents a build version on specific channel. Possible values:
    - latest - most latest build on specific channel
    - coherent - most latest coherent build on specific channel
          coherent applies only to SDK downloads
    - 3-part version in a format A.B.C - represents specific version of build
          examples: 2.0.0-preview2-006120, 1.1.0
.PARAMETER InstallDir
    Default: %LocalAppData%\Microsoft\dotnet
    Path to where to install dotnet. Note that binaries will be placed directly in a given directory.
.PARAMETER Architecture
    Default: <auto> - this value represents currently running OS architecture
    Architecture of dotnet binaries to be installed.
    Possible values are: <auto>, amd64, x64, x86, arm64, arm
.PARAMETER SharedRuntime
    This parameter is obsolete and may be removed in a future version of this script.
    The recommended alternative is '-Runtime dotnet'.
    Installs just the shared runtime bits, not the entire SDK.
.PARAMETER Runtime
    Installs just a shared runtime, not the entire SDK.
    Possible values:
        - dotnet     - the Microsoft.NETCore.App shared runtime
        - aspnetcore - the Microsoft.AspNetCore.App shared runtime
        - windowsdesktop - the Microsoft.WindowsDesktop.App shared runtime
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
    This parameter typically is not changed by the user.
    It allows changing the URL for the Azure feed used by this installer.
.PARAMETER UncachedFeed
    This parameter typically is not changed by the user.
    It allows changing the URL for the Uncached feed used by this installer.
.PARAMETER FeedCredential
    Used as a query string to append to the Azure feed.
    It allows changing the URL to use non-public blob storage accounts.
.PARAMETER ProxyAddress
    If set, the installer will use the proxy when making web requests
.PARAMETER ProxyUseDefaultCredentials
    Default: false
    Use default credentials, when using proxy address.
.PARAMETER SkipNonVersionedFiles
    Default: false
    Skips installing non-versioned files if they already exist, such as dotnet.exe.
.PARAMETER NoCdn
    Disable downloading from the Azure CDN, and use the uncached feed directly.
.PARAMETER JSonFile
    Determines the SDK version from a user specified global.json file
    Note: global.json must have a value for 'SDK:Version'
#>
[cmdletbinding()]
param(
   [string]$Channel="LTS",
   [string]$Version="Latest",
   [string]$JSonFile,
   [string]$InstallDir="<auto>",
   [string]$Architecture="<auto>",
   [ValidateSet("dotnet", "aspnetcore", "windowsdesktop", IgnoreCase = $false)]
   [string]$Runtime,
   [Obsolete("This parameter may be removed in a future version of this script. The recommended alternative is '-Runtime dotnet'.")]
   [switch]$SharedRuntime,
   [switch]$DryRun,
   [switch]$NoPath,
   [string]$AzureFeed="https://dotnetcli.azureedge.net/dotnet",
   [string]$UncachedFeed="https://dotnetcli.blob.core.windows.net/dotnet",
   [string]$FeedCredential,
   [string]$ProxyAddress,
   [switch]$ProxyUseDefaultCredentials,
   [switch]$SkipNonVersionedFiles,
   [switch]$NoCdn
)

Set-StrictMode -Version Latest
$ErrorActionPreference="Stop"
$ProgressPreference="SilentlyContinue"

if ($NoCdn) {
    $AzureFeed = $UncachedFeed
}

$BinFolderRelativePath=""

if ($SharedRuntime -and (-not $Runtime)) {
    $Runtime = "dotnet"
}

# example path with regex: shared/1.0.0-beta-12345/somepath
$VersionRegEx="/\d+\.\d+[^/]+/"
$OverrideNonVersionedFiles = !$SkipNonVersionedFiles

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

function Invoke-With-Retry([ScriptBlock]$ScriptBlock, [int]$MaxAttempts = 3, [int]$SecondsBetweenAttempts = 1) {
    $Attempts = 0

    while ($true) {
        try {
            return $ScriptBlock.Invoke()
        }
        catch {
            $Attempts++
            if ($Attempts -lt $MaxAttempts) {
                Start-Sleep $SecondsBetweenAttempts
            }
            else {
                throw
            }
        }
    }
}

function Get-Machine-Architecture() {
    Say-Invocation $MyInvocation

    # possible values: amd64, x64, x86, arm64, arm
    return $ENV:PROCESSOR_ARCHITECTURE
}

function Get-CLIArchitecture-From-Architecture([string]$Architecture) {
    Say-Invocation $MyInvocation

    switch ($Architecture.ToLower()) {
        { $_ -eq "<auto>" } { return Get-CLIArchitecture-From-Architecture $(Get-Machine-Architecture) }
        { ($_ -eq "amd64") -or ($_ -eq "x64") } { return "x64" }
        { $_ -eq "x86" } { return "x86" }
        { $_ -eq "arm" } { return "arm" }
        { $_ -eq "arm64" } { return "arm64" }
        default { throw "Architecture not supported. If you think this is a bug, report it at https://github.com/dotnet/sdk/issues" }
    }
}

# The version text returned from the feeds is a 1-line or 2-line string:
# For the SDK and the dotnet runtime (2 lines):
# Line 1: # commit_hash
# Line 2: # 4-part version
# For the aspnetcore runtime (1 line):
# Line 1: # 4-part version
function Get-Version-Info-From-Version-Text([string]$VersionText) {
    Say-Invocation $MyInvocation

    $Data = -split $VersionText

    $VersionInfo = @{
        CommitHash = $(if ($Data.Count -gt 1) { $Data[0] })
        Version = $Data[-1] # last line is always the version number.
    }
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
    Invoke-With-Retry(
    {

        $HttpClient = $null

        try {
            # HttpClient is used vs Invoke-WebRequest in order to support Nano Server which doesn't support the Invoke-WebRequest cmdlet.
            Load-Assembly -Assembly System.Net.Http

            if(-not $ProxyAddress) {
                try {
                    # Despite no proxy being explicitly specified, we may still be behind a default proxy
                    $DefaultProxy = [System.Net.WebRequest]::DefaultWebProxy;
                    if($DefaultProxy -and (-not $DefaultProxy.IsBypassed($Uri))) {
                        $ProxyAddress = $DefaultProxy.GetProxy($Uri).OriginalString
                        $ProxyUseDefaultCredentials = $true
                    }
                } catch {
                    # Eat the exception and move forward as the above code is an attempt
                    #    at resolving the DefaultProxy that may not have been a problem.
                    $ProxyAddress = $null
                    Say-Verbose("Exception ignored: $_.Exception.Message - moving forward...")
                }
            }

            if($ProxyAddress) {
                $HttpClientHandler = New-Object System.Net.Http.HttpClientHandler
                $HttpClientHandler.Proxy =  New-Object System.Net.WebProxy -Property @{Address=$ProxyAddress;UseDefaultCredentials=$ProxyUseDefaultCredentials}
                $HttpClient = New-Object System.Net.Http.HttpClient -ArgumentList $HttpClientHandler
            }
            else {

                $HttpClient = New-Object System.Net.Http.HttpClient
            }
            # Default timeout for HttpClient is 100s.  For a 50 MB download this assumes 500 KB/s average, any less will time out
            # 20 minutes allows it to work over much slower connections.
            $HttpClient.Timeout = New-TimeSpan -Minutes 20
            $Response = $HttpClient.GetAsync("${Uri}${FeedCredential}").Result
            if (($Response -eq $null) -or (-not ($Response.IsSuccessStatusCode))) {
                 # The feed credential is potentially sensitive info. Do not log FeedCredential to console output.
                $ErrorMsg = "Failed to download $Uri."
                if ($Response -ne $null) {
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
    })
}

function Get-Latest-Version-Info([string]$AzureFeed, [string]$Channel, [bool]$Coherent) {
    Say-Invocation $MyInvocation

    $VersionFileUrl = $null
    if ($Runtime -eq "dotnet") {
        $VersionFileUrl = "$UncachedFeed/Runtime/$Channel/latest.version"
    }
    elseif ($Runtime -eq "aspnetcore") {
        $VersionFileUrl = "$UncachedFeed/aspnetcore/Runtime/$Channel/latest.version"
    }
    # Currently, the WindowsDesktop runtime is manufactured with the .Net core runtime
    elseif ($Runtime -eq "windowsdesktop") {
        $VersionFileUrl = "$UncachedFeed/Runtime/$Channel/latest.version"
    }
    elseif (-not $Runtime) {
        if ($Coherent) {
            $VersionFileUrl = "$UncachedFeed/Sdk/$Channel/latest.coherent.version"
        }
        else {
            $VersionFileUrl = "$UncachedFeed/Sdk/$Channel/latest.version"
        }
    }
    else {
        throw "Invalid value for `$Runtime"
    }
    try {
        $Response = GetHTTPResponse -Uri $VersionFileUrl
    }
    catch {
        throw "Could not resolve version information."
    }
    $StringContent = $Response.Content.ReadAsStringAsync().Result

    switch ($Response.Content.Headers.ContentType) {
        { ($_ -eq "application/octet-stream") } { $VersionText = $StringContent }
        { ($_ -eq "text/plain") } { $VersionText = $StringContent }
        { ($_ -eq "text/plain; charset=UTF-8") } { $VersionText = $StringContent }
        default { throw "``$Response.Content.Headers.ContentType`` is an unknown .version file content type." }
    }

    $VersionInfo = Get-Version-Info-From-Version-Text $VersionText

    return $VersionInfo
}

function Parse-Jsonfile-For-Version([string]$JSonFile) {
    Say-Invocation $MyInvocation

    If (-Not (Test-Path $JSonFile)) {
        throw "Unable to find '$JSonFile'"
    }
    try {
        $JSonContent = Get-Content($JSonFile) -Raw | ConvertFrom-Json | Select-Object -expand "sdk" -ErrorAction SilentlyContinue
    }
    catch {
        throw "Json file unreadable: '$JSonFile'"
    }
    if ($JSonContent) {
        try {
            $JSonContent.PSObject.Properties | ForEach-Object {
                $PropertyName = $_.Name
                if ($PropertyName -eq "version") {
                    $Version = $_.Value
                    Say-Verbose "Version = $Version"
                }
            }
        }
        catch {
            throw "Unable to parse the SDK node in '$JSonFile'"
        }
    }
    else {
        throw "Unable to find the SDK node in '$JSonFile'"
    }
    If ($Version -eq $null) {
        throw "Unable to find the SDK:version node in '$JSonFile'"
    }
    return $Version
}

function Get-Specific-Version-From-Version([string]$AzureFeed, [string]$Channel, [string]$Version, [string]$JSonFile) {
    Say-Invocation $MyInvocation

    if (-not $JSonFile) {
        switch ($Version.ToLower()) {
            { $_ -eq "latest" } {
                $LatestVersionInfo = Get-Latest-Version-Info -AzureFeed $AzureFeed -Channel $Channel -Coherent $False
                return $LatestVersionInfo.Version
            }
            { $_ -eq "coherent" } {
                $LatestVersionInfo = Get-Latest-Version-Info -AzureFeed $AzureFeed -Channel $Channel -Coherent $True
                return $LatestVersionInfo.Version
            }
            default { return $Version }
        }
    }
    else {
        return Parse-Jsonfile-For-Version $JSonFile
    }
}

function Get-Download-Link([string]$AzureFeed, [string]$SpecificVersion, [string]$CLIArchitecture) {
    Say-Invocation $MyInvocation

    if ($Runtime -eq "dotnet") {
        $PayloadURL = "$AzureFeed/Runtime/$SpecificVersion/dotnet-runtime-$SpecificVersion-win-$CLIArchitecture.zip"
    }
    elseif ($Runtime -eq "aspnetcore") {
        $PayloadURL = "$AzureFeed/aspnetcore/Runtime/$SpecificVersion/aspnetcore-runtime-$SpecificVersion-win-$CLIArchitecture.zip"
    }
    elseif ($Runtime -eq "windowsdesktop") {
        $PayloadURL = "$AzureFeed/Runtime/$SpecificVersion/windowsdesktop-runtime-$SpecificVersion-win-$CLIArchitecture.zip"
    }
    elseif (-not $Runtime) {
        $PayloadURL = "$AzureFeed/Sdk/$SpecificVersion/dotnet-sdk-$SpecificVersion-win-$CLIArchitecture.zip"
    }
    else {
        throw "Invalid value for `$Runtime"
    }

    Say-Verbose "Constructed primary named payload URL: $PayloadURL"

    return $PayloadURL
}

function Get-LegacyDownload-Link([string]$AzureFeed, [string]$SpecificVersion, [string]$CLIArchitecture) {
    Say-Invocation $MyInvocation

    if (-not $Runtime) {
        $PayloadURL = "$AzureFeed/Sdk/$SpecificVersion/dotnet-dev-win-$CLIArchitecture.$SpecificVersion.zip"
    }
    elseif ($Runtime -eq "dotnet") {
        $PayloadURL = "$AzureFeed/Runtime/$SpecificVersion/dotnet-win-$CLIArchitecture.$SpecificVersion.zip"
    }
    else {
        return $null
    }

    Say-Verbose "Constructed legacy named payload URL: $PayloadURL"

    return $PayloadURL
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

function Is-Dotnet-Package-Installed([string]$InstallRoot, [string]$RelativePathToPackage, [string]$SpecificVersion) {
    Say-Invocation $MyInvocation

    $DotnetPackagePath = Join-Path -Path $InstallRoot -ChildPath $RelativePathToPackage | Join-Path -ChildPath $SpecificVersion
    Say-Verbose "Is-Dotnet-Package-Installed: DotnetPackagePath=$DotnetPackagePath"
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

function DownloadFile($Source, [string]$OutPath) {
    if ($Source -notlike "http*") {
        #  Using System.IO.Path.GetFullPath to get the current directory
        #    does not work in this context - $pwd gives the current directory
        if (![System.IO.Path]::IsPathRooted($Source)) {
            $Source = $(Join-Path -Path $pwd -ChildPath $Source)
        }
        $Source = Get-Absolute-Path $Source
        Say "Copying file from $Source to $OutPath"
        Copy-Item $Source $OutPath
        return
    }

    $Stream = $null

    try {
        $Response = GetHTTPResponse -Uri $Source
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

function Prepend-Sdk-InstallRoot-To-Path([string]$InstallRoot, [string]$BinFolderRelativePath) {
    $BinPath = Get-Absolute-Path $(Join-Path -Path $InstallRoot -ChildPath $BinFolderRelativePath)
    if (-Not $NoPath) {
        $SuffixedBinPath = "$BinPath;"
        if (-Not $env:path.Contains($SuffixedBinPath)) {
            Say "Adding to current process PATH: `"$BinPath`". Note: This change will not be visible if PowerShell was run as a child process."
            $env:path = $SuffixedBinPath + $env:path
        } else {
            Say-Verbose "Current process PATH already contains `"$BinPath`""
        }
    }
    else {
        Say "Binaries of dotnet can be found in $BinPath"
    }
}

$CLIArchitecture = Get-CLIArchitecture-From-Architecture $Architecture
$SpecificVersion = Get-Specific-Version-From-Version -AzureFeed $AzureFeed -Channel $Channel -Version $Version -JSonFile $JSonFile
$DownloadLink = Get-Download-Link -AzureFeed $AzureFeed -SpecificVersion $SpecificVersion -CLIArchitecture $CLIArchitecture
$LegacyDownloadLink = Get-LegacyDownload-Link -AzureFeed $AzureFeed -SpecificVersion $SpecificVersion -CLIArchitecture $CLIArchitecture

$InstallRoot = Resolve-Installation-Path $InstallDir
Say-Verbose "InstallRoot: $InstallRoot"
$ScriptName = $MyInvocation.MyCommand.Name

if ($DryRun) {
    Say "Payload URLs:"
    Say "Primary named payload URL: $DownloadLink"
    if ($LegacyDownloadLink) {
        Say "Legacy named payload URL: $LegacyDownloadLink"
    }
    $RepeatableCommand = ".\$ScriptName -Version `"$SpecificVersion`" -InstallDir `"$InstallRoot`" -Architecture `"$CLIArchitecture`""
    if ($Runtime -eq "dotnet") {
       $RepeatableCommand+=" -Runtime `"dotnet`""
    }
    elseif ($Runtime -eq "aspnetcore") {
       $RepeatableCommand+=" -Runtime `"aspnetcore`""
    }
    foreach ($key in $MyInvocation.BoundParameters.Keys) {
        if (-not (@("Architecture","Channel","DryRun","InstallDir","Runtime","SharedRuntime","Version") -contains $key)) {
            $RepeatableCommand+=" -$key `"$($MyInvocation.BoundParameters[$key])`""
        }
    }
    Say "Repeatable invocation: $RepeatableCommand"
    exit 0
}

if ($Runtime -eq "dotnet") {
    $assetName = ".NET Core Runtime"
    $dotnetPackageRelativePath = "shared\Microsoft.NETCore.App"
}
elseif ($Runtime -eq "aspnetcore") {
    $assetName = "ASP.NET Core Runtime"
    $dotnetPackageRelativePath = "shared\Microsoft.AspNetCore.App"
}
elseif ($Runtime -eq "windowsdesktop") {
    $assetName = ".NET Core Windows Desktop Runtime"
    $dotnetPackageRelativePath = "shared\Microsoft.WindowsDesktop.App"
}
elseif (-not $Runtime) {
    $assetName = ".NET Core SDK"
    $dotnetPackageRelativePath = "sdk"
}
else {
    throw "Invalid value for `$Runtime"
}

#  Check if the SDK version is already installed.
$isAssetInstalled = Is-Dotnet-Package-Installed -InstallRoot $InstallRoot -RelativePathToPackage $dotnetPackageRelativePath -SpecificVersion $SpecificVersion
if ($isAssetInstalled) {
    Say "$assetName version $SpecificVersion is already installed."
    Prepend-Sdk-InstallRoot-To-Path -InstallRoot $InstallRoot -BinFolderRelativePath $BinFolderRelativePath
    exit 0
}

New-Item -ItemType Directory -Force -Path $InstallRoot | Out-Null

$installDrive = $((Get-Item $InstallRoot).PSDrive.Name);
$diskInfo = Get-PSDrive -Name $installDrive
if ($diskInfo.Free / 1MB -le 100) {
    Say "There is not enough disk space on drive ${installDrive}:"
    exit 0
}

$ZipPath = [System.IO.Path]::combine([System.IO.Path]::GetTempPath(), [System.IO.Path]::GetRandomFileName())
Say-Verbose "Zip path: $ZipPath"

$DownloadFailed = $false
Say "Downloading link: $DownloadLink"
try {
    DownloadFile -Source $DownloadLink -OutPath $ZipPath
}
catch {
    Say "Cannot download: $DownloadLink"
    if ($LegacyDownloadLink) {
        $DownloadLink = $LegacyDownloadLink
        $ZipPath = [System.IO.Path]::combine([System.IO.Path]::GetTempPath(), [System.IO.Path]::GetRandomFileName())
        Say-Verbose "Legacy zip path: $ZipPath"
        Say "Downloading legacy link: $DownloadLink"
        try {
            DownloadFile -Source $DownloadLink -OutPath $ZipPath
        }
        catch {
            Say "Cannot download: $DownloadLink"
            $DownloadFailed = $true
        }
    }
    else {
        $DownloadFailed = $true
    }
}

if ($DownloadFailed) {
    throw "Could not find/download: `"$assetName`" with version = $SpecificVersion`nRefer to: https://aka.ms/dotnet-os-lifecycle for information on .NET Core support"
}

Say "Extracting zip from $DownloadLink"
Extract-Dotnet-Package -ZipPath $ZipPath -OutPath $InstallRoot

#  Check if the SDK version is installed; if not, fail the installation.
$isAssetInstalled = $false

# if the version contains "RTM" or "servicing"; check if a 'release-type' SDK version is installed.
if ($SpecificVersion -Match "rtm" -or $SpecificVersion -Match "servicing") {
    $ReleaseVersion = $SpecificVersion.Split("-")[0]
    Say-Verbose "Checking installation: version = $ReleaseVersion"
    $isAssetInstalled = Is-Dotnet-Package-Installed -InstallRoot $InstallRoot -RelativePathToPackage $dotnetPackageRelativePath -SpecificVersion $ReleaseVersion
}

#  Check if the SDK version is installed.
if (!$isAssetInstalled) {
    Say-Verbose "Checking installation: version = $SpecificVersion"
    $isAssetInstalled = Is-Dotnet-Package-Installed -InstallRoot $InstallRoot -RelativePathToPackage $dotnetPackageRelativePath -SpecificVersion $SpecificVersion
}

if (!$isAssetInstalled) {
    throw "`"$assetName`" with version = $SpecificVersion failed to install with an unknown error."
}

Remove-Item $ZipPath

Prepend-Sdk-InstallRoot-To-Path -InstallRoot $InstallRoot -BinFolderRelativePath $BinFolderRelativePath

Say "Installation finished"
exit 0

# SIG # Begin signature block
# MIIjkQYJKoZIhvcNAQcCoIIjgjCCI34CAQExDzANBglghkgBZQMEAgEFADB5Bgor
# BgEEAYI3AgEEoGswaTA0BgorBgEEAYI3AgEeMCYCAwEAAAQQH8w7YFlLCE63JNLG
# KX7zUQIBAAIBAAIBAAIBAAIBADAxMA0GCWCGSAFlAwQCAQUABCAwp4UsNdAkvwY3
# VhbuN9D6NGOz+qNqW2+62YubWa4qJaCCDYEwggX/MIID56ADAgECAhMzAAABh3IX
# chVZQMcJAAAAAAGHMA0GCSqGSIb3DQEBCwUAMH4xCzAJBgNVBAYTAlVTMRMwEQYD
# VQQIEwpXYXNoaW5ndG9uMRAwDgYDVQQHEwdSZWRtb25kMR4wHAYDVQQKExVNaWNy
# b3NvZnQgQ29ycG9yYXRpb24xKDAmBgNVBAMTH01pY3Jvc29mdCBDb2RlIFNpZ25p
# bmcgUENBIDIwMTEwHhcNMjAwMzA0MTgzOTQ3WhcNMjEwMzAzMTgzOTQ3WjB0MQsw
# CQYDVQQGEwJVUzETMBEGA1UECBMKV2FzaGluZ3RvbjEQMA4GA1UEBxMHUmVkbW9u
# ZDEeMBwGA1UEChMVTWljcm9zb2Z0IENvcnBvcmF0aW9uMR4wHAYDVQQDExVNaWNy
# b3NvZnQgQ29ycG9yYXRpb24wggEiMA0GCSqGSIb3DQEBAQUAA4IBDwAwggEKAoIB
# AQDOt8kLc7P3T7MKIhouYHewMFmnq8Ayu7FOhZCQabVwBp2VS4WyB2Qe4TQBT8aB
# znANDEPjHKNdPT8Xz5cNali6XHefS8i/WXtF0vSsP8NEv6mBHuA2p1fw2wB/F0dH
# sJ3GfZ5c0sPJjklsiYqPw59xJ54kM91IOgiO2OUzjNAljPibjCWfH7UzQ1TPHc4d
# weils8GEIrbBRb7IWwiObL12jWT4Yh71NQgvJ9Fn6+UhD9x2uk3dLj84vwt1NuFQ
# itKJxIV0fVsRNR3abQVOLqpDugbr0SzNL6o8xzOHL5OXiGGwg6ekiXA1/2XXY7yV
# Fc39tledDtZjSjNbex1zzwSXAgMBAAGjggF+MIIBejAfBgNVHSUEGDAWBgorBgEE
# AYI3TAgBBggrBgEFBQcDAzAdBgNVHQ4EFgQUhov4ZyO96axkJdMjpzu2zVXOJcsw
# UAYDVR0RBEkwR6RFMEMxKTAnBgNVBAsTIE1pY3Jvc29mdCBPcGVyYXRpb25zIFB1
# ZXJ0byBSaWNvMRYwFAYDVQQFEw0yMzAwMTIrNDU4Mzg1MB8GA1UdIwQYMBaAFEhu
# ZOVQBdOCqhc3NyK1bajKdQKVMFQGA1UdHwRNMEswSaBHoEWGQ2h0dHA6Ly93d3cu
# bWljcm9zb2Z0LmNvbS9wa2lvcHMvY3JsL01pY0NvZFNpZ1BDQTIwMTFfMjAxMS0w
# Ny0wOC5jcmwwYQYIKwYBBQUHAQEEVTBTMFEGCCsGAQUFBzAChkVodHRwOi8vd3d3
# Lm1pY3Jvc29mdC5jb20vcGtpb3BzL2NlcnRzL01pY0NvZFNpZ1BDQTIwMTFfMjAx
# MS0wNy0wOC5jcnQwDAYDVR0TAQH/BAIwADANBgkqhkiG9w0BAQsFAAOCAgEAixmy
# S6E6vprWD9KFNIB9G5zyMuIjZAOuUJ1EK/Vlg6Fb3ZHXjjUwATKIcXbFuFC6Wr4K
# NrU4DY/sBVqmab5AC/je3bpUpjtxpEyqUqtPc30wEg/rO9vmKmqKoLPT37svc2NV
# BmGNl+85qO4fV/w7Cx7J0Bbqk19KcRNdjt6eKoTnTPHBHlVHQIHZpMxacbFOAkJr
# qAVkYZdz7ikNXTxV+GRb36tC4ByMNxE2DF7vFdvaiZP0CVZ5ByJ2gAhXMdK9+usx
# zVk913qKde1OAuWdv+rndqkAIm8fUlRnr4saSCg7cIbUwCCf116wUJ7EuJDg0vHe
# yhnCeHnBbyH3RZkHEi2ofmfgnFISJZDdMAeVZGVOh20Jp50XBzqokpPzeZ6zc1/g
# yILNyiVgE+RPkjnUQshd1f1PMgn3tns2Cz7bJiVUaqEO3n9qRFgy5JuLae6UweGf
# AeOo3dgLZxikKzYs3hDMaEtJq8IP71cX7QXe6lnMmXU/Hdfz2p897Zd+kU+vZvKI
# 3cwLfuVQgK2RZ2z+Kc3K3dRPz2rXycK5XCuRZmvGab/WbrZiC7wJQapgBodltMI5
# GMdFrBg9IeF7/rP4EqVQXeKtevTlZXjpuNhhjuR+2DMt/dWufjXpiW91bo3aH6Ea
# jOALXmoxgltCp1K7hrS6gmsvj94cLRf50QQ4U8Qwggd6MIIFYqADAgECAgphDpDS
# AAAAAAADMA0GCSqGSIb3DQEBCwUAMIGIMQswCQYDVQQGEwJVUzETMBEGA1UECBMK
# V2FzaGluZ3RvbjEQMA4GA1UEBxMHUmVkbW9uZDEeMBwGA1UEChMVTWljcm9zb2Z0
# IENvcnBvcmF0aW9uMTIwMAYDVQQDEylNaWNyb3NvZnQgUm9vdCBDZXJ0aWZpY2F0
# ZSBBdXRob3JpdHkgMjAxMTAeFw0xMTA3MDgyMDU5MDlaFw0yNjA3MDgyMTA5MDla
# MH4xCzAJBgNVBAYTAlVTMRMwEQYDVQQIEwpXYXNoaW5ndG9uMRAwDgYDVQQHEwdS
# ZWRtb25kMR4wHAYDVQQKExVNaWNyb3NvZnQgQ29ycG9yYXRpb24xKDAmBgNVBAMT
# H01pY3Jvc29mdCBDb2RlIFNpZ25pbmcgUENBIDIwMTEwggIiMA0GCSqGSIb3DQEB
# AQUAA4ICDwAwggIKAoICAQCr8PpyEBwurdhuqoIQTTS68rZYIZ9CGypr6VpQqrgG
# OBoESbp/wwwe3TdrxhLYC/A4wpkGsMg51QEUMULTiQ15ZId+lGAkbK+eSZzpaF7S
# 35tTsgosw6/ZqSuuegmv15ZZymAaBelmdugyUiYSL+erCFDPs0S3XdjELgN1q2jz
# y23zOlyhFvRGuuA4ZKxuZDV4pqBjDy3TQJP4494HDdVceaVJKecNvqATd76UPe/7
# 4ytaEB9NViiienLgEjq3SV7Y7e1DkYPZe7J7hhvZPrGMXeiJT4Qa8qEvWeSQOy2u
# M1jFtz7+MtOzAz2xsq+SOH7SnYAs9U5WkSE1JcM5bmR/U7qcD60ZI4TL9LoDho33
# X/DQUr+MlIe8wCF0JV8YKLbMJyg4JZg5SjbPfLGSrhwjp6lm7GEfauEoSZ1fiOIl
# XdMhSz5SxLVXPyQD8NF6Wy/VI+NwXQ9RRnez+ADhvKwCgl/bwBWzvRvUVUvnOaEP
# 6SNJvBi4RHxF5MHDcnrgcuck379GmcXvwhxX24ON7E1JMKerjt/sW5+v/N2wZuLB
# l4F77dbtS+dJKacTKKanfWeA5opieF+yL4TXV5xcv3coKPHtbcMojyyPQDdPweGF
# RInECUzF1KVDL3SV9274eCBYLBNdYJWaPk8zhNqwiBfenk70lrC8RqBsmNLg1oiM
# CwIDAQABo4IB7TCCAekwEAYJKwYBBAGCNxUBBAMCAQAwHQYDVR0OBBYEFEhuZOVQ
# BdOCqhc3NyK1bajKdQKVMBkGCSsGAQQBgjcUAgQMHgoAUwB1AGIAQwBBMAsGA1Ud
# DwQEAwIBhjAPBgNVHRMBAf8EBTADAQH/MB8GA1UdIwQYMBaAFHItOgIxkEO5FAVO
# 4eqnxzHRI4k0MFoGA1UdHwRTMFEwT6BNoEuGSWh0dHA6Ly9jcmwubWljcm9zb2Z0
# LmNvbS9wa2kvY3JsL3Byb2R1Y3RzL01pY1Jvb0NlckF1dDIwMTFfMjAxMV8wM18y
# Mi5jcmwwXgYIKwYBBQUHAQEEUjBQME4GCCsGAQUFBzAChkJodHRwOi8vd3d3Lm1p
# Y3Jvc29mdC5jb20vcGtpL2NlcnRzL01pY1Jvb0NlckF1dDIwMTFfMjAxMV8wM18y
# Mi5jcnQwgZ8GA1UdIASBlzCBlDCBkQYJKwYBBAGCNy4DMIGDMD8GCCsGAQUFBwIB
# FjNodHRwOi8vd3d3Lm1pY3Jvc29mdC5jb20vcGtpb3BzL2RvY3MvcHJpbWFyeWNw
# cy5odG0wQAYIKwYBBQUHAgIwNB4yIB0ATABlAGcAYQBsAF8AcABvAGwAaQBjAHkA
# XwBzAHQAYQB0AGUAbQBlAG4AdAAuIB0wDQYJKoZIhvcNAQELBQADggIBAGfyhqWY
# 4FR5Gi7T2HRnIpsLlhHhY5KZQpZ90nkMkMFlXy4sPvjDctFtg/6+P+gKyju/R6mj
# 82nbY78iNaWXXWWEkH2LRlBV2AySfNIaSxzzPEKLUtCw/WvjPgcuKZvmPRul1LUd
# d5Q54ulkyUQ9eHoj8xN9ppB0g430yyYCRirCihC7pKkFDJvtaPpoLpWgKj8qa1hJ
# Yx8JaW5amJbkg/TAj/NGK978O9C9Ne9uJa7lryft0N3zDq+ZKJeYTQ49C/IIidYf
# wzIY4vDFLc5bnrRJOQrGCsLGra7lstnbFYhRRVg4MnEnGn+x9Cf43iw6IGmYslmJ
# aG5vp7d0w0AFBqYBKig+gj8TTWYLwLNN9eGPfxxvFX1Fp3blQCplo8NdUmKGwx1j
# NpeG39rz+PIWoZon4c2ll9DuXWNB41sHnIc+BncG0QaxdR8UvmFhtfDcxhsEvt9B
# xw4o7t5lL+yX9qFcltgA1qFGvVnzl6UJS0gQmYAf0AApxbGbpT9Fdx41xtKiop96
# eiL6SJUfq/tHI4D1nvi/a7dLl+LrdXga7Oo3mXkYS//WsyNodeav+vyL6wuA6mk7
# r/ww7QRMjt/fdW1jkT3RnVZOT7+AVyKheBEyIXrvQQqxP/uozKRdwaGIm1dxVk5I
# RcBCyZt2WwqASGv9eZ/BvW1taslScxMNelDNMYIVZjCCFWICAQEwgZUwfjELMAkG
# A1UEBhMCVVMxEzARBgNVBAgTCldhc2hpbmd0b24xEDAOBgNVBAcTB1JlZG1vbmQx
# HjAcBgNVBAoTFU1pY3Jvc29mdCBDb3Jwb3JhdGlvbjEoMCYGA1UEAxMfTWljcm9z
# b2Z0IENvZGUgU2lnbmluZyBQQ0EgMjAxMQITMwAAAYdyF3IVWUDHCQAAAAABhzAN
# BglghkgBZQMEAgEFAKCBrjAZBgkqhkiG9w0BCQMxDAYKKwYBBAGCNwIBBDAcBgor
# BgEEAYI3AgELMQ4wDAYKKwYBBAGCNwIBFTAvBgkqhkiG9w0BCQQxIgQga11B1DE+
# y9z0lmEO+MC+bhXPKfWALB7Snkn7G/wCUncwQgYKKwYBBAGCNwIBDDE0MDKgFIAS
# AE0AaQBjAHIAbwBzAG8AZgB0oRqAGGh0dHA6Ly93d3cubWljcm9zb2Z0LmNvbTAN
# BgkqhkiG9w0BAQEFAASCAQBIgx+sFXkLXf7Xbx7opCD3uhpQGEQ4x/LsqTax0bu1
# GC/cxiI+dodUz+T4hKj1ZQyUH0Zlce32GutY048O9tkr7fQyuohoFUgChdIATEOY
# qAIESFbDT07i7khJfO2pewlhgM+A5ClvBa8HAvV0wOd+2IVgv3pgow1LEJm0/5NB
# E3IFA+hFrqiWALOY0uUep4H20EHMrbqw3YoV3EodIkTj3fC76q4K/bF84EZLUgjY
# e4rmXac8n7A9qR18QzGl8usEJej4OHU4nlUT1J734m+AWIFmfb/Zr2MyXED0V4q4
# Vbmw3O7xD9STeNYrn5RjPmGPEN04akHxhNUSqLIc9vxQoYIS8DCCEuwGCisGAQQB
# gjcDAwExghLcMIIS2AYJKoZIhvcNAQcCoIISyTCCEsUCAQMxDzANBglghkgBZQME
# AgEFADCCAVQGCyqGSIb3DQEJEAEEoIIBQwSCAT8wggE7AgEBBgorBgEEAYRZCgMB
# MDEwDQYJYIZIAWUDBAIBBQAEIPPK1A0D1n7ZEdgTjKPY4sWiOMtohMqGpFvG55NY
# SFHeAgZepuJh/dEYEjIwMjAwNTI5MTYyNzE1LjMxWjAEgAIB9KCB1KSB0TCBzjEL
# MAkGA1UEBhMCVVMxEzARBgNVBAgTCldhc2hpbmd0b24xEDAOBgNVBAcTB1JlZG1v
# bmQxHjAcBgNVBAoTFU1pY3Jvc29mdCBDb3Jwb3JhdGlvbjEpMCcGA1UECxMgTWlj
# cm9zb2Z0IE9wZXJhdGlvbnMgUHVlcnRvIFJpY28xJjAkBgNVBAsTHVRoYWxlcyBU
# U1MgRVNOOjYwQkMtRTM4My0yNjM1MSUwIwYDVQQDExxNaWNyb3NvZnQgVGltZS1T
# dGFtcCBTZXJ2aWNloIIORDCCBPUwggPdoAMCAQICEzMAAAEm37pLIrmCggcAAAAA
# ASYwDQYJKoZIhvcNAQELBQAwfDELMAkGA1UEBhMCVVMxEzARBgNVBAgTCldhc2hp
# bmd0b24xEDAOBgNVBAcTB1JlZG1vbmQxHjAcBgNVBAoTFU1pY3Jvc29mdCBDb3Jw
# b3JhdGlvbjEmMCQGA1UEAxMdTWljcm9zb2Z0IFRpbWUtU3RhbXAgUENBIDIwMTAw
# HhcNMTkxMjE5MDExNDU5WhcNMjEwMzE3MDExNDU5WjCBzjELMAkGA1UEBhMCVVMx
# EzARBgNVBAgTCldhc2hpbmd0b24xEDAOBgNVBAcTB1JlZG1vbmQxHjAcBgNVBAoT
# FU1pY3Jvc29mdCBDb3Jwb3JhdGlvbjEpMCcGA1UECxMgTWljcm9zb2Z0IE9wZXJh
# dGlvbnMgUHVlcnRvIFJpY28xJjAkBgNVBAsTHVRoYWxlcyBUU1MgRVNOOjYwQkMt
# RTM4My0yNjM1MSUwIwYDVQQDExxNaWNyb3NvZnQgVGltZS1TdGFtcCBTZXJ2aWNl
# MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAnjC+hpxO8w2VdBO18X8L
# Hk6XdfR9yNQ0y+MuBOY7n5YdgkVunvbk/f6q8UoNFAdYQjVLPSAHbi6tUMiNeMGH
# k1U0lUxAkja2W2/szj/ghuFklvfHNBbsuiUShlhRlqcFNS7KXL2iwKDijmOhWJPY
# a2bLEr4W/mQLbSXail5p6m138Ttx4MAVEzzuGI0Kwr8ofIL7z6zCeWDiBM57LrNC
# qHOA2wboeuMsG4O0Oz2LMAzBLbJZPRPnZAD2HdD4HUL2mzZ8wox74Mekb7RzrUP3
# hiHpxXZceJvhIEKfAgVkB5kTZQnio8A1JijMjw8f4TmsJPdJWpi8ei73sexe8/Yj
# cwIDAQABo4IBGzCCARcwHQYDVR0OBBYEFEmrrB8XsH6YQo3RWKZfxqM0DmFBMB8G
# A1UdIwQYMBaAFNVjOlyKMZDzQ3t8RhvFM2hahW1VMFYGA1UdHwRPME0wS6BJoEeG
# RWh0dHA6Ly9jcmwubWljcm9zb2Z0LmNvbS9wa2kvY3JsL3Byb2R1Y3RzL01pY1Rp
# bVN0YVBDQV8yMDEwLTA3LTAxLmNybDBaBggrBgEFBQcBAQROMEwwSgYIKwYBBQUH
# MAKGPmh0dHA6Ly93d3cubWljcm9zb2Z0LmNvbS9wa2kvY2VydHMvTWljVGltU3Rh
# UENBXzIwMTAtMDctMDEuY3J0MAwGA1UdEwEB/wQCMAAwEwYDVR0lBAwwCgYIKwYB
# BQUHAwgwDQYJKoZIhvcNAQELBQADggEBAECW+51o6W/0J/O/npudfjVzMXq0u0cs
# HjqXpdRyH6o03jlmY5MXAui3cmPBKufijJxD2pMRPVMUNh3VA0PQuJeYrP06oFdq
# LpLxd3IJARm98vzaMgCz2nCwBDpe9X2M3Js9K1GAX+w4Az8N7J+Z6P1OD0VxHBdq
# eTaqDN1lk1vwagTN7t/WitxMXRDz0hRdYiWbATBAVgXXCOfzs3hnEv1n/EDab9HX
# OLMXKVY/+alqYKdV9lkuRp8Us1Q1WZy9z72Azu9x4mzft3fJ1puTjBHo5tHfixZo
# ummbI+WwjVCrku7pskJahfNi5amSgrqgR6nWAwvpJELccpVLdSxxmG0wggZxMIIE
# WaADAgECAgphCYEqAAAAAAACMA0GCSqGSIb3DQEBCwUAMIGIMQswCQYDVQQGEwJV
# UzETMBEGA1UECBMKV2FzaGluZ3RvbjEQMA4GA1UEBxMHUmVkbW9uZDEeMBwGA1UE
# ChMVTWljcm9zb2Z0IENvcnBvcmF0aW9uMTIwMAYDVQQDEylNaWNyb3NvZnQgUm9v
# dCBDZXJ0aWZpY2F0ZSBBdXRob3JpdHkgMjAxMDAeFw0xMDA3MDEyMTM2NTVaFw0y
# NTA3MDEyMTQ2NTVaMHwxCzAJBgNVBAYTAlVTMRMwEQYDVQQIEwpXYXNoaW5ndG9u
# MRAwDgYDVQQHEwdSZWRtb25kMR4wHAYDVQQKExVNaWNyb3NvZnQgQ29ycG9yYXRp
# b24xJjAkBgNVBAMTHU1pY3Jvc29mdCBUaW1lLVN0YW1wIFBDQSAyMDEwMIIBIjAN
# BgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAqR0NvHcRijog7PwTl/X6f2mUa3RU
# ENWlCgCChfvtfGhLLF/Fw+Vhwna3PmYrW/AVUycEMR9BGxqVHc4JE458YTBZsTBE
# D/FgiIRUQwzXTbg4CLNC3ZOs1nMwVyaCo0UN0Or1R4HNvyRgMlhgRvJYR4YyhB50
# YWeRX4FUsc+TTJLBxKZd0WETbijGGvmGgLvfYfxGwScdJGcSchohiq9LZIlQYrFd
# /XcfPfBXday9ikJNQFHRD5wGPmd/9WbAA5ZEfu/QS/1u5ZrKsajyeioKMfDaTgaR
# togINeh4HLDpmc085y9Euqf03GS9pAHBIAmTeM38vMDJRF1eFpwBBU8iTQIDAQAB
# o4IB5jCCAeIwEAYJKwYBBAGCNxUBBAMCAQAwHQYDVR0OBBYEFNVjOlyKMZDzQ3t8
# RhvFM2hahW1VMBkGCSsGAQQBgjcUAgQMHgoAUwB1AGIAQwBBMAsGA1UdDwQEAwIB
# hjAPBgNVHRMBAf8EBTADAQH/MB8GA1UdIwQYMBaAFNX2VsuP6KJcYmjRPZSQW9fO
# mhjEMFYGA1UdHwRPME0wS6BJoEeGRWh0dHA6Ly9jcmwubWljcm9zb2Z0LmNvbS9w
# a2kvY3JsL3Byb2R1Y3RzL01pY1Jvb0NlckF1dF8yMDEwLTA2LTIzLmNybDBaBggr
# BgEFBQcBAQROMEwwSgYIKwYBBQUHMAKGPmh0dHA6Ly93d3cubWljcm9zb2Z0LmNv
# bS9wa2kvY2VydHMvTWljUm9vQ2VyQXV0XzIwMTAtMDYtMjMuY3J0MIGgBgNVHSAB
# Af8EgZUwgZIwgY8GCSsGAQQBgjcuAzCBgTA9BggrBgEFBQcCARYxaHR0cDovL3d3
# dy5taWNyb3NvZnQuY29tL1BLSS9kb2NzL0NQUy9kZWZhdWx0Lmh0bTBABggrBgEF
# BQcCAjA0HjIgHQBMAGUAZwBhAGwAXwBQAG8AbABpAGMAeQBfAFMAdABhAHQAZQBt
# AGUAbgB0AC4gHTANBgkqhkiG9w0BAQsFAAOCAgEAB+aIUQ3ixuCYP4FxAz2do6Eh
# b7Prpsz1Mb7PBeKp/vpXbRkws8LFZslq3/Xn8Hi9x6ieJeP5vO1rVFcIK1GCRBL7
# uVOMzPRgEop2zEBAQZvcXBf/XPleFzWYJFZLdO9CEMivv3/Gf/I3fVo/HPKZeUqR
# UgCvOA8X9S95gWXZqbVr5MfO9sp6AG9LMEQkIjzP7QOllo9ZKby2/QThcJ8ySif9
# Va8v/rbljjO7Yl+a21dA6fHOmWaQjP9qYn/dxUoLkSbiOewZSnFjnXshbcOco6I8
# +n99lmqQeKZt0uGc+R38ONiU9MalCpaGpL2eGq4EQoO4tYCbIjggtSXlZOz39L9+
# Y1klD3ouOVd2onGqBooPiRa6YacRy5rYDkeagMXQzafQ732D8OE7cQnfXXSYIghh
# 2rBQHm+98eEA3+cxB6STOvdlR3jo+KhIq/fecn5ha293qYHLpwmsObvsxsvYgrRy
# zR30uIUBHoD7G4kqVDmyW9rIDVWZeodzOwjmmC3qjeAzLhIp9cAvVCch98isTtoo
# uLGp25ayp0Kiyc8ZQU3ghvkqmqMRZjDTu3QyS99je/WZii8bxyGvWbWu3EQ8l1Bx
# 16HSxVXjad5XwdHeMMD9zOZN+w2/XU/pnR4ZOC+8z1gFLu8NoFA12u8JJxzVs341
# Hgi62jbb01+P3nSISRKhggLSMIICOwIBATCB/KGB1KSB0TCBzjELMAkGA1UEBhMC
# VVMxEzARBgNVBAgTCldhc2hpbmd0b24xEDAOBgNVBAcTB1JlZG1vbmQxHjAcBgNV
# BAoTFU1pY3Jvc29mdCBDb3Jwb3JhdGlvbjEpMCcGA1UECxMgTWljcm9zb2Z0IE9w
# ZXJhdGlvbnMgUHVlcnRvIFJpY28xJjAkBgNVBAsTHVRoYWxlcyBUU1MgRVNOOjYw
# QkMtRTM4My0yNjM1MSUwIwYDVQQDExxNaWNyb3NvZnQgVGltZS1TdGFtcCBTZXJ2
# aWNloiMKAQEwBwYFKw4DAhoDFQAKZzI5aZnESumrToHx3Lqgxnr//KCBgzCBgKR+
# MHwxCzAJBgNVBAYTAlVTMRMwEQYDVQQIEwpXYXNoaW5ndG9uMRAwDgYDVQQHEwdS
# ZWRtb25kMR4wHAYDVQQKExVNaWNyb3NvZnQgQ29ycG9yYXRpb24xJjAkBgNVBAMT
# HU1pY3Jvc29mdCBUaW1lLVN0YW1wIFBDQSAyMDEwMA0GCSqGSIb3DQEBBQUAAgUA
# 4nuQTDAiGA8yMDIwMDUyOTE3NDQ0NFoYDzIwMjAwNTMwMTc0NDQ0WjB3MD0GCisG
# AQQBhFkKBAExLzAtMAoCBQDie5BMAgEAMAoCAQACAiZJAgH/MAcCAQACAhEjMAoC
# BQDifOHMAgEAMDYGCisGAQQBhFkKBAIxKDAmMAwGCisGAQQBhFkKAwKgCjAIAgEA
# AgMHoSChCjAIAgEAAgMBhqAwDQYJKoZIhvcNAQEFBQADgYEAprmyJTXdH9FmQZ0I
# mRSJdjc/RrSqDm8DUEq/h3FL73G/xvg9MbQj1J/h3hdlSIPcQXjrhL8hud/vyF0j
# IFaTK5YOcixkX++9t7Vz3Mn0KkQo8F4DNSyZEPpz682AyKKwLMJDy52pFFFKNP5l
# NpOz6YY1Od1xvk4nyN1WwfLnGswxggMNMIIDCQIBATCBkzB8MQswCQYDVQQGEwJV
# UzETMBEGA1UECBMKV2FzaGluZ3RvbjEQMA4GA1UEBxMHUmVkbW9uZDEeMBwGA1UE
# ChMVTWljcm9zb2Z0IENvcnBvcmF0aW9uMSYwJAYDVQQDEx1NaWNyb3NvZnQgVGlt
# ZS1TdGFtcCBQQ0EgMjAxMAITMwAAASbfuksiuYKCBwAAAAABJjANBglghkgBZQME
# AgEFAKCCAUowGgYJKoZIhvcNAQkDMQ0GCyqGSIb3DQEJEAEEMC8GCSqGSIb3DQEJ
# BDEiBCB0IE0Q6P23RQlh8TFyp57UQQUF/sbui7mOMStRgTFZxTCB+gYLKoZIhvcN
# AQkQAi8xgeowgecwgeQwgb0EIDb9z++evV5wDO9qk5ZnbEZ8CTOuR+kZyu8xbTsJ
# CXUPMIGYMIGApH4wfDELMAkGA1UEBhMCVVMxEzARBgNVBAgTCldhc2hpbmd0b24x
# EDAOBgNVBAcTB1JlZG1vbmQxHjAcBgNVBAoTFU1pY3Jvc29mdCBDb3Jwb3JhdGlv
# bjEmMCQGA1UEAxMdTWljcm9zb2Z0IFRpbWUtU3RhbXAgUENBIDIwMTACEzMAAAEm
# 37pLIrmCggcAAAAAASYwIgQgtwi02bvsGAOdpAxEF607G6g9PlyS8vc2bAUSHovH
# /IIwDQYJKoZIhvcNAQELBQAEggEAEMCfsXNudrjztjI6JNyNDVpdF1axRVcGiNy6
# 67pgb1EePsjA2EaBB+5ZjgO/73JxuiVgsoXgH7em8tKG5RQJtcm5obVDb+jKksK4
# qcFLA1f7seQRGfE06UAPnSFh2GqMtTNJGCXWwqWLH2LduTjOqPt8Nupo16ABFIT2
# akTzBSJ81EHBkEU0Et6CgeaZiBYrCCXUtD+ASvLDkPSrjweQGu3Zk1SSROEzxMY9
# jdlGfMkK2krMd9ub9UZ13RcQDijJqo+h6mz76pAuiFFvuQl6wMoSGFaaUQwfd+WQ
# gXlVVX/A9JFBihrxnDVglEPlsIOxCHkTeIxLfnAkCbax+9pevA==
# SIG # End signature block
