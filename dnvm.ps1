#Requires -Version 2

if (Test-Path env:WEBSITE_SITE_NAME)
{
    # This script is run in Azure Web Sites
    # Disable progress indicator
    $ProgressPreference = "SilentlyContinue"
}

$ScriptPath = $MyInvocation.MyCommand.Definition

$Script:UseWriteHost = $true
function _WriteDebug($msg) {
    if($Script:UseWriteHost) {
        try {
            Write-Debug $msg
        } catch {
            $Script:UseWriteHost = $false
            _WriteDebug $msg
        }
    }
}

function _WriteOut {
    param(
        [Parameter(Mandatory=$false, Position=0, ValueFromPipeline=$true)][string]$msg,
        [Parameter(Mandatory=$false)][ConsoleColor]$ForegroundColor,
        [Parameter(Mandatory=$false)][ConsoleColor]$BackgroundColor,
        [Parameter(Mandatory=$false)][switch]$NoNewLine)

    if($__TestWriteTo) {
        $cur = Get-Variable -Name $__TestWriteTo -ValueOnly -Scope Global -ErrorAction SilentlyContinue
        $val = $cur + "$msg"
        if(!$NoNewLine) {
            $val += [Environment]::NewLine
        }
        Set-Variable -Name $__TestWriteTo -Value $val -Scope Global -Force
        return
    }

    if(!$Script:UseWriteHost) {
        if(!$msg) {
            $msg = ""
        }
        if($NoNewLine) {
            [Console]::Write($msg)
        } else {
            [Console]::WriteLine($msg)
        }
    }
    else {
        try {
            if(!$ForegroundColor) {
                $ForegroundColor = $host.UI.RawUI.ForegroundColor
            }
            if(!$BackgroundColor) {
                $BackgroundColor = $host.UI.RawUI.BackgroundColor
            }

            Write-Host $msg -ForegroundColor:$ForegroundColor -BackgroundColor:$BackgroundColor -NoNewLine:$NoNewLine
        } catch {
            $Script:UseWriteHost = $false
            _WriteOut $msg
        }
    }
}

### Constants
$ProductVersion="1.0.0"
$BuildVersion="rc2-15546"
$Authors="Microsoft Open Technologies, Inc."

# If the Version hasn't been replaced...
# We can't compare directly with the build version token
# because it'll just get replaced here as well :)
if($BuildVersion.StartsWith("{{")) {
    # We're being run from source code rather than the "compiled" artifact
    $BuildVersion = "HEAD"
}
$FullVersion="$ProductVersion-$BuildVersion"

Set-Variable -Option Constant "CommandName" ([IO.Path]::GetFileNameWithoutExtension($ScriptPath))
Set-Variable -Option Constant "CommandFriendlyName" ".NET Version Manager"
Set-Variable -Option Constant "DefaultUserDirectoryName" ".dnx"
Set-Variable -Option Constant "DefaultGlobalDirectoryName" "Microsoft DNX"
Set-Variable -Option Constant "OldUserDirectoryNames" @(".kre", ".k")
Set-Variable -Option Constant "RuntimePackageName" "dnx"
Set-Variable -Option Constant "DefaultFeed" "https://www.nuget.org/api/v2"
Set-Variable -Option Constant "DefaultFeedKey" "DNX_FEED"
Set-Variable -Option Constant "DefaultUnstableFeed" "https://www.myget.org/F/aspnetvnext/api/v2"
Set-Variable -Option Constant "DefaultUnstableFeedKey" "DNX_UNSTABLE_FEED"
Set-Variable -Option Constant "CrossGenCommand" "dnx-crossgen"
Set-Variable -Option Constant "OldCrossGenCommand" "k-crossgen"
Set-Variable -Option Constant "CommandPrefix" "dnvm-"
Set-Variable -Option Constant "DefaultArchitecture" "x86"
Set-Variable -Option Constant "DefaultRuntime" "clr"
Set-Variable -Option Constant "AliasExtension" ".txt"
Set-Variable -Option Constant "DefaultOperatingSystem" "win"

# These are intentionally using "%" syntax. The environment variables are expanded whenever the value is used.
Set-Variable -Option Constant "OldUserHomes" @("%USERPROFILE%\.kre", "%USERPROFILE%\.k")
Set-Variable -Option Constant "DefaultUserHome" "%USERPROFILE%\$DefaultUserDirectoryName"
Set-Variable -Option Constant "HomeEnvVar" "DNX_HOME"

Set-Variable -Option Constant "RuntimeShortFriendlyName" "DNX"

Set-Variable -Option Constant "DNVMUpgradeUrl" "https://raw.githubusercontent.com/aspnet/Home/dev/dnvm.ps1"

Set-Variable -Option Constant "AsciiArt" @"
   ___  _  ___   ____  ___
  / _ \/ |/ / | / /  |/  /
 / // /    /| |/ / /|_/ /
/____/_/|_/ |___/_/  /_/
"@

$ExitCodes = @{
    "Success"                   = 0
    "AliasDoesNotExist"         = 1001
    "UnknownCommand"            = 1002
    "InvalidArguments"          = 1003
    "OtherError"                = 1004
    "NoSuchPackage"             = 1005
    "NoRuntimesOnFeed"          = 1006
}

$ColorScheme = $DnvmColors
if(!$ColorScheme) {
    $ColorScheme = @{
        "Banner"=[ConsoleColor]::Cyan
        "RuntimeName"=[ConsoleColor]::Yellow
        "Help_Header"=[ConsoleColor]::Yellow
        "Help_Switch"=[ConsoleColor]::Green
        "Help_Argument"=[ConsoleColor]::Cyan
        "Help_Optional"=[ConsoleColor]::Gray
        "Help_Command"=[ConsoleColor]::DarkYellow
        "Help_Executable"=[ConsoleColor]::DarkYellow
        "Feed_Name"=[ConsoleColor]::Cyan
        "Warning" = [ConsoleColor]::Yellow
        "Error" = [ConsoleColor]::Red
        "ActiveRuntime" = [ConsoleColor]::Cyan
    }
}

Set-Variable -Option Constant "OptionPadding" 20
Set-Variable -Option Constant "CommandPadding" 15

# Test Control Variables
if($__TeeTo) {
    _WriteDebug "Saving output to '$__TeeTo' variable"
    Set-Variable -Name $__TeeTo -Value "" -Scope Global -Force
}

# Commands that have been deprecated but do still work.
$DeprecatedCommands = @("unalias")

# Load Environment variables
$RuntimeHomes = $(if (Test-Path "env:\$HomeEnvVar") {Get-Content "env:\$HomeEnvVar"})
$UserHome = $env:DNX_USER_HOME
$GlobalHome = $env:DNX_GLOBAL_HOME
$ActiveFeed = $(if (Test-Path "env:\$DefaultFeedKey") {Get-Content "env:\$DefaultFeedKey"})
$ActiveUnstableFeed = $(if (Test-Path "env:\$DefaultUnstableFeedKey") {Get-Content "env:\$DefaultUnstableFeedKey"})

# Default Exit Code
$Script:ExitCode = $ExitCodes.Success

############################################################
### Below this point, the terms "DNVM", "DNX", etc.      ###
### should never be used. Instead, use the Constants     ###
### defined above                                        ###
############################################################
# An exception to the above: The commands are defined by functions
# named "dnvm-[command name]" so that extension functions can be added

$StartPath = $env:PATH

if($CmdPathFile) {
    if(Test-Path $CmdPathFile) {
        _WriteDebug "Cleaning old CMD PATH file: $CmdPathFile"
        Remove-Item $CmdPathFile -Force
    }
    _WriteDebug "Using CMD PATH file: $CmdPathFile"
}

# Determine the default installation directory (UserHome)
if(!$UserHome) {
    if ($RuntimeHomes) {
      _WriteDebug "Detecting User Home..."
      $pf = $env:ProgramFiles
      if(Test-Path "env:\ProgramFiles(x86)") {
          $pf32 = Get-Content "env:\ProgramFiles(x86)"
      }

      # Canonicalize so we can do StartsWith tests
      if(!$pf.EndsWith("\")) { $pf += "\" }
      if($pf32 -and !$pf32.EndsWith("\")) { $pf32 += "\" }

      $UserHome = $RuntimeHomes.Split(";") | Where-Object {
          # Take the first path that isn't under program files
          !($_.StartsWith($pf) -or $_.StartsWith($pf32))
      } | Select-Object -First 1

      _WriteDebug "Found: $UserHome"
    }

    if(!$UserHome) {
        $UserHome = "$DefaultUserHome"
    }
}
$UserHome = [Environment]::ExpandEnvironmentVariables($UserHome)

# Determine the default global installation directory (GlobalHome)
if(!$GlobalHome) {
    if($env:ProgramData) {
        $GlobalHome = "$env:ProgramData\$DefaultGlobalDirectoryName"
    } else {
        $GlobalHome = "$env:AllUsersProfile\$DefaultGlobalDirectoryName"
    }
}
$GlobalHome = [Environment]::ExpandEnvironmentVariables($GlobalHome)

# Determine where runtimes can exist (RuntimeHomes)
if(!$RuntimeHomes) {
    # Set up a default value for the runtime home
    $UnencodedHomes = "$UserHome;$GlobalHome"
} elseif ($RuntimeHomes.StartsWith(';')) {
    _WriteOut "Ignoring invalid $HomeEnvVar; value was '$RuntimeHomes'" -ForegroundColor $ColorScheme.Warning
    Clean-HomeEnv($true)

    # Use default instead.
    $UnencodedHomes = "$UserHome;$GlobalHome"
} else {
    $UnencodedHomes = $RuntimeHomes
}

$UnencodedHomes = $UnencodedHomes.Split(";")
$RuntimeHomes = $UnencodedHomes | ForEach-Object { [Environment]::ExpandEnvironmentVariables($_) }
$RuntimeDirs = $RuntimeHomes | ForEach-Object { Join-Path $_ "runtimes" }

_WriteDebug ""
_WriteDebug "=== Running $CommandName ==="
_WriteDebug "Runtime Homes: $RuntimeHomes"
_WriteDebug "User Home: $UserHome"
$AliasesDir = Join-Path $UserHome "alias"
$RuntimesDir = Join-Path $UserHome "runtimes"
$GlobalRuntimesDir = Join-Path $GlobalHome "runtimes"
$Aliases = $null

### Helper Functions
# Remove $HomeEnv from process and user environment.
# Called when current value is invalid or after installing files to default location.
function Clean-HomeEnv {
    param([switch]$SkipUserEnvironment)

    if (Test-Path "env:\$HomeEnvVar") {
        _WriteOut "Removing Process $HomeEnvVar"
        Set-Content "env:\$HomeEnvVar" $null
    }

    if (!$SkipUserEnvironment -and [Environment]::GetEnvironmentVariable($HomeEnvVar, "User")) {
        _WriteOut "Removing User $HomeEnvVar"
        [Environment]::SetEnvironmentVariable($HomeEnvVar, $null, "User")
    }
}

# Checks if a specified file exists in the destination folder and if not, copies the file
# to the destination folder.
function Safe-Filecopy {
    param(
        [Parameter(Mandatory=$true, Position=0)] $Filename,
        [Parameter(Mandatory=$true, Position=1)] $SourceFolder,
        [Parameter(Mandatory=$true, Position=2)] $DestinationFolder)

    # Make sure the destination folder is created if it doesn't already exist.
    if(!(Test-Path $DestinationFolder)) {
        _WriteOut "Creating destination folder '$DestinationFolder' ... "

        New-Item -Type Directory $Destination | Out-Null
    }

    $sourceFilePath = Join-Path $SourceFolder $Filename
    $destFilePath = Join-Path $DestinationFolder $Filename

    if(Test-Path $sourceFilePath) {
        _WriteOut "Installing '$Filename' to '$DestinationFolder' ... "

        if (Test-Path $destFilePath) {
            _WriteOut "  Skipping: file already exists" -ForegroundColor Yellow
        }
        else {
            Copy-Item $sourceFilePath $destFilePath -Force
        }
    }
    else {
        _WriteOut "WARNING: Unable to install: Could not find '$Filename' in '$SourceFolder'. "
    }
}

$OSRuntimeDefaults = @{
    "win"="clr";
    "linux"="mono";
    "darwin"="mono";
}

$RuntimeBitnessDefaults = @{
    "clr"="x86";
    "coreclr"="x64";
}

function GetRuntimeInfo($Architecture, $Runtime, $OS, $Version) {
    $runtimeInfo = @{
        "Architecture"="$Architecture";
        "Runtime"="$Runtime";
        "OS"="$OS";
        "Version"="$Version";
    }

    if([String]::IsNullOrEmpty($runtimeInfo.OS)) {
        if($runtimeInfo.Runtime -eq "mono"){
            #If OS is empty and you are asking for mono, i.e `dnvm install latest -os mono` then we don't know what OS to pick. It could be Linux or Darwin.
            #we could just arbitrarily pick one but it will probably be wrong as often as not.
            #If Mono can run on Windows then this error doesn't make sense anymore.
            throw "Unable to determine an operating system for a $($runtimeInfo.Runtime) runtime. You must specify which OS to use with the OS parameter."
        }
        $runtimeInfo.OS = $DefaultOperatingSystem
    }

    if($runtimeInfo.OS -eq "osx") {
        $runtimeInfo.OS = "darwin"
    }

    if([String]::IsNullOrEmpty($runtimeInfo.Runtime)) {
        $runtimeInfo.Runtime = $OSRuntimeDefaults.Get_Item($runtimeInfo.OS)
    }

    if([String]::IsNullOrEmpty($runtimeInfo.Architecture)) {
        $runtimeInfo.Architecture = $RuntimeBitnessDefaults.Get_Item($RuntimeInfo.Runtime)
    }

    $runtimeObject = New-Object PSObject -Property $runtimeInfo

    $runtimeObject | Add-Member -MemberType ScriptProperty -Name RuntimeId -Value {
        if($this.Runtime -eq "mono") {
            "$RuntimePackageName-$($this.Runtime)".ToLowerInvariant()
        } else {
            "$RuntimePackageName-$($this.Runtime)-$($this.OS)-$($this.Architecture)".ToLowerInvariant()
        }
    }

    $runtimeObject | Add-Member -MemberType ScriptProperty -Name RuntimeName -Value {
        "$($this.RuntimeId).$($this.Version)"
    }

    $runtimeObject
}

function Write-Usage {
    _WriteOut -ForegroundColor $ColorScheme.Banner $AsciiArt
    _WriteOut "$CommandFriendlyName v$FullVersion"
    if(!$Authors.StartsWith("{{")) {
        _WriteOut "By $Authors"
    }
    _WriteOut -NoNewLine -ForegroundColor $ColorScheme.Help_Header "usage:"
    _WriteOut -NoNewLine -ForegroundColor $ColorScheme.Help_Executable " $CommandName"
    _WriteOut -NoNewLine -ForegroundColor $ColorScheme.Help_Command " <command>"
    _WriteOut -ForegroundColor $ColorScheme.Help_Argument " [<arguments...>]"
}

function Write-Feeds {
    _WriteOut
    _WriteOut -ForegroundColor $ColorScheme.Help_Header "Current feed settings:"
    _WriteOut -NoNewline -ForegroundColor $ColorScheme.Feed_Name "Default Stable: "
    _WriteOut "$DefaultFeed"
    _WriteOut -NoNewline -ForegroundColor $ColorScheme.Feed_Name "Default Unstable: "
    _WriteOut "$DefaultUnstableFeed"
    _WriteOut -NoNewline -ForegroundColor $ColorScheme.Feed_Name "Current Stable Override: "
    if($ActiveFeed) {
        _WriteOut "$ActiveFeed"
    } else {
        _WriteOut "<none>"
    }
    _WriteOut -NoNewline -ForegroundColor $ColorScheme.Feed_Name "Current Unstable Override: "
    if($ActiveUnstableFeed) {
        _WriteOut "$ActiveUnstableFeed"
    } else {
        _WriteOut "<none>"
    }
    _WriteOut
    _WriteOut -NoNewline "    To use override feeds, set "
    _WriteOut -NoNewLine -ForegroundColor $ColorScheme.Help_Executable "$DefaultFeedKey"
    _WriteOut -NoNewline " and "
    _WriteOut -NoNewLine -ForegroundColor $ColorScheme.Help_Executable "$DefaultUnstableFeedKey"
    _WriteOut -NoNewline " environment keys respectively"
    _WriteOut
}

function Get-RuntimeAlias {
    if($Aliases -eq $null) {
        _WriteDebug "Scanning for aliases in $AliasesDir"
        if(Test-Path $AliasesDir) {
            $Aliases = @(Get-ChildItem ($UserHome + "\alias\") | Select-Object @{label='Alias';expression={$_.BaseName}}, @{label='Name';expression={Get-Content $_.FullName }}, @{label='Orphan';expression={-Not (Test-Path ($RuntimesDir + "\" + (Get-Content $_.FullName)))}})
        } else {
            $Aliases = @()
        }
    }
    $Aliases
}

function IsOnPath {
    param($dir)

    $env:Path.Split(';') -icontains $dir
}

function Get-RuntimeAliasOrRuntimeInfo(
    [Parameter(Mandatory=$true)][string]$Version,
    [Parameter()][string]$Architecture,
    [Parameter()][string]$Runtime,
    [Parameter()][string]$OS) {

    $aliasPath = Join-Path $AliasesDir "$Version$AliasExtension"

    if(Test-Path $aliasPath) {
        $BaseName = Get-Content $aliasPath

        if(!$Architecture) {
            $Architecture = Get-PackageArch $BaseName
        }
        if(!$Runtime) {
            $Runtime = Get-PackageRuntime $BaseName
        }
        $Version = Get-PackageVersion $BaseName
        $OS = Get-PackageOS $BaseName
    }

    GetRuntimeInfo $Architecture $Runtime $OS $Version
}

filter List-Parts {
    param($aliases, $items)

    $location = ""

    $binDir = Join-Path $_.FullName "bin"
    if ((Test-Path $binDir)) {
        $location = $_.Parent.FullName
    }
    $active = IsOnPath $binDir

    $fullAlias=""
    $delim=""

    foreach($alias in $aliases) {
        if($_.Name.Split('\', 2) -contains $alias.Name) {
            $fullAlias += $delim + $alias.Alias + (&{if($alias.Orphan){" (missing)"}})
            $delim = ", "
        }
    }

    $parts1 = $_.Name.Split('.', 2)
    $parts2 = $parts1[0].Split('-', 4)

    if($parts1[0] -eq "$RuntimePackageName-mono") {
        $parts2 += "linux/osx"
        $parts2 += "x86/x64"
    }

    $aliasUsed = ""
    if($items) {
    $aliasUsed = $items | ForEach-Object {
        if($_.Architecture -eq $parts2[3] -and $_.Runtime -eq $parts2[1] -and $_.OperatingSystem -eq $parts2[2] -and $_.Version -eq $parts1[1]) {
            return $true;
        }
        return $false;
    }
    }

    if($aliasUsed -eq $true) {
        $fullAlias = ""
    }

    return New-Object PSObject -Property @{
        Active = $active
        Version = $parts1[1]
        Runtime = $parts2[1]
        OperatingSystem = $parts2[2]
        Architecture = $parts2[3]
        Location = $location
        Alias = $fullAlias
    }
}

function Read-Alias($Name) {
    _WriteDebug "Listing aliases matching '$Name'"

    $aliases = Get-RuntimeAlias

    $result = @($aliases | Where-Object { !$Name -or ($_.Alias.Contains($Name)) })
    if($Name -and ($result.Length -eq 1)) {
        _WriteOut "Alias '$Name' is set to '$($result[0].Name)'"
    } elseif($Name -and ($result.Length -eq 0)) {
        _WriteOut "Alias does not exist: '$Name'"
        $Script:ExitCode = $ExitCodes.AliasDoesNotExist
    } else {
        $result
    }
}

function Write-Alias {
    param(
        [Parameter(Mandatory=$true)][string]$Name,
        [Parameter(Mandatory=$true)][string]$Version,
        [Parameter(Mandatory=$false)][string]$Architecture,
        [Parameter(Mandatory=$false)][string]$Runtime,
        [Parameter(Mandatory=$false)][string]$OS)

    # If the first character is non-numeric, it's a full runtime name
    if(![Char]::IsDigit($Version[0])) {
        $runtimeInfo = GetRuntimeInfo $(Get-PackageArch $Version) $(Get-PackageRuntime $Version) $(Get-PackageOS $Version) $(Get-PackageVersion $Version)
    } else {
        $runtimeInfo = GetRuntimeInfo $Architecture $Runtime $OS $Version
    }

    $aliasFilePath = Join-Path $AliasesDir "$Name.txt"
    $action = if (Test-Path $aliasFilePath) { "Updating" } else { "Setting" }

    if(!(Test-Path $AliasesDir)) {
        _WriteDebug "Creating alias directory: $AliasesDir"
        New-Item -Type Directory $AliasesDir | Out-Null
    }
    _WriteOut "$action alias '$Name' to '$($runtimeInfo.RuntimeName)'"
    $runtimeInfo.RuntimeName | Out-File $aliasFilePath ascii
}

function Delete-Alias {
    param(
        [Parameter(Mandatory=$true)][string]$Name)

    $aliasPath = Join-Path $AliasesDir "$Name.txt"
    if (Test-Path -literalPath "$aliasPath") {
        _WriteOut "Removing alias $Name"

        # Delete with "-Force" because we already confirmed above
        Remove-Item -literalPath $aliasPath -Force
    } else {
        _WriteOut "Cannot remove alias '$Name'. It does not exist."
        $Script:ExitCode = $ExitCodes.AliasDoesNotExist # Return non-zero exit code for scripting
    }
}

function Apply-Proxy {
param(
  [System.Net.WebClient] $wc,
  [string]$Proxy
)
  if (!$Proxy) {
    $Proxy = $env:http_proxy
  }
  if ($Proxy) {
    $wp = New-Object System.Net.WebProxy($Proxy)
    $pb = New-Object UriBuilder($Proxy)
    if (!$pb.UserName) {
        $wp.Credentials = [System.Net.CredentialCache]::DefaultCredentials
    } else {
        $wp.Credentials = New-Object System.Net.NetworkCredential($pb.UserName, $pb.Password)
    }
    $wc.Proxy = $wp
  }
}

function Find-Package {
    param(
        $runtimeInfo,
        [string]$Feed,
        [string]$Proxy
    )
    $url = "$Feed/Packages()?`$filter=Id eq '$($runtimeInfo.RuntimeId)' and Version eq '$($runtimeInfo.Version)'"
    Invoke-NuGetWebRequest $runtimeInfo.RuntimeId $url $Proxy
}

function Find-Latest {
    param(
        $runtimeInfo,
        [Parameter(Mandatory=$true)]
        [string]$Feed,
        [string]$Proxy
    )

    _WriteOut "Determining latest version"
    $RuntimeId = $runtimeInfo.RuntimeId
    _WriteDebug "Latest RuntimeId: $RuntimeId"
    $url = "$Feed/GetUpdates()?packageIds=%27$RuntimeId%27&versions=%270.0%27&includePrerelease=true&includeAllVersions=false"
    Invoke-NuGetWebRequest $RuntimeId $url $Proxy
}

function Invoke-NuGetWebRequest {
    param (
        [string]$RuntimeId,
        [string]$Url,
        [string]$Proxy
    )
    # NOTE: DO NOT use Invoke-WebRequest. It requires PowerShell 4.0!

    $wc = New-Object System.Net.WebClient
    Apply-Proxy $wc -Proxy:$Proxy
    _WriteDebug "Downloading $Url ..."
    try {
        [xml]$xml = $wc.DownloadString($Url)
    } catch {
        $Script:ExitCode = $ExitCodes.NoRuntimesOnFeed
        throw "Unable to find any runtime packages on the feed!"
    }

    $version = Select-Xml "//d:Version" -Namespace @{d='http://schemas.microsoft.com/ado/2007/08/dataservices'} $xml
    if($version) {
        $downloadUrl = (Select-Xml "//d:content/@src" -Namespace @{d='http://www.w3.org/2005/Atom'} $xml).Node.value
        _WriteDebug "Found $version at $downloadUrl"
        @{ Version = $version; DownloadUrl = $downloadUrl }
    } else {
        throw "There are no runtimes matching the name $RuntimeId on feed $feed."
    }
}

function Get-PackageVersion() {
    param(
        [string] $runtimeFullName
    )
    return $runtimeFullName -replace '[^.]*.(.*)', '$1'
}

function Get-PackageRuntime() {
    param(
        [string] $runtimeFullName
    )
    return $runtimeFullName -replace "$RuntimePackageName-([^-]*).*", '$1'
}

function Get-PackageArch() {
    param(
        [string] $runtimeFullName
    )
    return $runtimeFullName -replace "$RuntimePackageName-[^-]*-[^-]*-([^.]*).*", '$1'
}

function Get-PackageOS() {
    param(
        [string] $runtimeFullName
    )
    $runtimeFullName -replace "$RuntimePackageName-[^-]*-([^-]*)-[^.]*.*", '$1'
}

function Download-Package() {
    param(
        $runtimeInfo,
        [Parameter(Mandatory=$true)]
        [string]$DownloadUrl,
        [string]$DestinationFile,
        [Parameter(Mandatory=$true)]
        [string]$Feed,
        [string]$Proxy
    )

    _WriteOut "Downloading $($runtimeInfo.RuntimeName) from $feed"
    $wc = New-Object System.Net.WebClient
    try {
      Apply-Proxy $wc -Proxy:$Proxy
      _WriteDebug "Downloading $DownloadUrl ..."

      Register-ObjectEvent $wc DownloadProgressChanged -SourceIdentifier WebClient.ProgressChanged -action {
        $Global:downloadData = $eventArgs
      } | Out-Null

      Register-ObjectEvent $wc DownloadFileCompleted -SourceIdentifier WebClient.ProgressComplete -action {
        $Global:downloadData = $eventArgs
        $Global:downloadCompleted = $true
      } | Out-Null

      $wc.DownloadFileAsync($DownloadUrl, $DestinationFile)

      while(-not $Global:downloadCompleted){
        $percent = $Global:downloadData.ProgressPercentage
        $totalBytes = $Global:downloadData.TotalBytesToReceive
        $receivedBytes = $Global:downloadData.BytesReceived
        If ($percent -ne $null) {
            Write-Progress -Activity ("Downloading $RuntimeShortFriendlyName from $DownloadUrl") `
                -Status ("Downloaded $($Global:downloadData.BytesReceived) of $($Global:downloadData.TotalBytesToReceive) bytes") `
                -PercentComplete $percent -Id 2 -ParentId 1
        }
      }

      if($Global:downloadData.Error) {
        if($Global:downloadData.Error.Response.StatusCode -eq [System.Net.HttpStatusCode]::NotFound){
            throw "The server returned a 404 (NotFound). This is most likely caused by the feed not having the version that you typed. Check that you typed the right version and try again. Other possible causes are the feed doesn't have a $RuntimeShortFriendlyName of the right name format or some other error caused a 404 on the server."
        } else {
            throw "Unable to download package: {0}" -f $Global:downloadData.Error.Message
        }
      }

      Write-Progress -Status "Done" -Activity ("Downloading $RuntimeShortFriendlyName from $DownloadUrl") -Id 2 -ParentId 1 -Completed
    }
    finally {
        Remove-Variable downloadData -Scope "Global"
        Remove-Variable downloadCompleted -Scope "Global"
        Unregister-Event -SourceIdentifier WebClient.ProgressChanged
        Unregister-Event -SourceIdentifier WebClient.ProgressComplete
        $wc.Dispose()
    }
}

function Unpack-Package([string]$DownloadFile, [string]$UnpackFolder) {
    _WriteDebug "Unpacking $DownloadFile to $UnpackFolder"

    $compressionLib = [System.Reflection.Assembly]::LoadWithPartialName('System.IO.Compression.FileSystem')

    if($compressionLib -eq $null) {
      try {
          # Shell will not recognize nupkg as a zip and throw, so rename it to zip
          $runtimeZip = [System.IO.Path]::ChangeExtension($DownloadFile, "zip")
          Rename-Item $DownloadFile $runtimeZip
          # Use the shell to uncompress the nupkg
          $shell_app=new-object -com shell.application
          $zip_file = $shell_app.namespace($runtimeZip)
          $destination = $shell_app.namespace($UnpackFolder)
          $destination.Copyhere($zip_file.items(), 0x14) #0x4 = don't show UI, 0x10 = overwrite files
      }
      finally {
        # Clean up the package file itself.
        Remove-Item $runtimeZip -Force
      }
    } else {
        [System.IO.Compression.ZipFile]::ExtractToDirectory($DownloadFile, $UnpackFolder)

        # Clean up the package file itself.
        Remove-Item $DownloadFile -Force
    }

    If (Test-Path -LiteralPath ($UnpackFolder + "\[Content_Types].xml")) {
        Remove-Item -LiteralPath ($UnpackFolder + "\[Content_Types].xml")
    }
    If (Test-Path ($UnpackFolder + "\_rels\")) {
        Remove-Item -LiteralPath ($UnpackFolder + "\_rels\") -Force -Recurse
    }
    If (Test-Path ($UnpackFolder + "\package\")) {
        Remove-Item -LiteralPath ($UnpackFolder + "\package\") -Force -Recurse
    }
}

function Get-RuntimePath($runtimeFullName) {
    _WriteDebug "Resolving $runtimeFullName"
    foreach($RuntimeHome in $RuntimeHomes) {
        $runtimeBin = "$RuntimeHome\runtimes\$runtimeFullName\bin"
        _WriteDebug " Candidate $runtimeBin"
        if (Test-Path $runtimeBin) {
            _WriteDebug " Found in $runtimeBin"
            return $runtimeBin
        }
    }
    return $null
}

function Change-Path() {
    param(
        [string] $existingPaths,
        [string] $prependPath,
        [string[]] $removePaths
    )
    _WriteDebug "Updating value to prepend '$prependPath' and remove '$removePaths'"

    $newPath = $prependPath
    foreach($portion in $existingPaths.Split(';')) {
        if(![string]::IsNullOrEmpty($portion)) {
            $skip = $portion -eq ""
            foreach($removePath in $removePaths) {
                if(![string]::IsNullOrEmpty($removePath)) {
                    $removePrefix = if($removePath.EndsWith("\")) { $removePath } else { "$removePath\" }

                    if ($removePath -and (($portion -eq $removePath) -or ($portion.StartsWith($removePrefix)))) {
                        _WriteDebug " Removing '$portion' because it matches '$removePath'"
                        $skip = $true
                    }
                }
            }
            if (!$skip) {
                if(![String]::IsNullOrEmpty($newPath)) {
                    $newPath += ";"
                }
                $newPath += $portion
            }
        }
    }
    return $newPath
}

function Set-Path() {
    param(
        [string] $newPath
    )

    $env:PATH = $newPath

    if($CmdPathFile) {
        $Parent = Split-Path -Parent $CmdPathFile
        if(!(Test-Path $Parent)) {
            New-Item -Type Directory $Parent -Force | Out-Null
        }
        _WriteDebug " Writing PATH file for CMD script"
        @"
SET "PATH=$newPath"
"@ | Out-File $CmdPathFile ascii
    }
}

function Ngen-Library(
    [Parameter(Mandatory=$true)]
    [string]$runtimeBin,

    [ValidateSet("x86", "x64")]
    [Parameter(Mandatory=$true)]
    [string]$architecture) {

    if ($architecture -eq 'x64') {
        $regView = [Microsoft.Win32.RegistryView]::Registry64
    }
    elseif ($architecture -eq 'x86') {
        $regView = [Microsoft.Win32.RegistryView]::Registry32
    }
    else {
        _WriteOut "Installation does not understand architecture $architecture, skipping ngen..."
        return
    }

    $regHive = [Microsoft.Win32.RegistryHive]::LocalMachine
    $regKey = [Microsoft.Win32.RegistryKey]::OpenBaseKey($regHive, $regView)
    $frameworkPath = $regKey.OpenSubKey("SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full").GetValue("InstallPath")
    $ngenExe = Join-Path $frameworkPath 'ngen.exe'

    $ngenCmds = ""
    foreach ($bin in Get-ChildItem $runtimeBin -Filter "Microsoft.CodeAnalysis.CSharp.dll") {
        $ngenCmds += "$ngenExe install $($bin.FullName);"
    }

    $ngenProc = Start-Process "$psHome\powershell.exe" -Verb runAs -ArgumentList "-ExecutionPolicy unrestricted & $ngenCmds" -Wait -PassThru -WindowStyle Hidden
}

function Is-Elevated() {
    $user = [Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()
    return $user.IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")
}

function Get-ScriptRoot() {
    if ($PSVersionTable.PSVersion.Major -ge 3) {
        return $PSScriptRoot
    }

    return Split-Path $script:MyInvocation.MyCommand.Path -Parent
}

### Commands

<#
.SYNOPSIS
    Updates DNVM to the latest version.
.PARAMETER Proxy
    Use the given address as a proxy when accessing remote server
#>
function dnvm-update-self {
    param(
        [Parameter(Mandatory=$false)]
        [string]$Proxy)

    _WriteOut "Updating $CommandName from $DNVMUpgradeUrl"
    $wc = New-Object System.Net.WebClient
    Apply-Proxy $wc -Proxy:$Proxy

    $CurrentScriptRoot = Get-ScriptRoot
    $dnvmFile = Join-Path $CurrentScriptRoot "dnvm.ps1"
    $tempDnvmFile = Join-Path $CurrentScriptRoot "temp"
    $backupFilePath = Join-Path $CurrentScriptRoot "dnvm.ps1.bak"

    $wc.DownloadFile($DNVMUpgradeUrl, $tempDnvmFile)

    if(Test-Path $backupFilePath) {
        Remove-Item $backupFilePath -Force
    }

    Rename-Item $dnvmFile $backupFilePath
    Rename-Item $tempDnvmFile $dnvmFile
}

<#
.SYNOPSIS
    Displays a list of commands, and help for specific commands
.PARAMETER Command
    A specific command to get help for
#>
function dnvm-help {
    [CmdletBinding(DefaultParameterSetName="GeneralHelp")]
    param(
        [Parameter(Mandatory=$true,Position=0,ParameterSetName="SpecificCommand")][string]$Command,
        [switch]$PassThru)

    if($Command) {
        $cmd = Get-Command "dnvm-$Command" -ErrorAction SilentlyContinue
        if(!$cmd) {
            _WriteOut "No such command: $Command"
            dnvm-help
            $Script:ExitCodes = $ExitCodes.UnknownCommand
            return
        }
        if($Host.Version.Major -lt 3) {
            $help = Get-Help "dnvm-$Command"
        } else {
            $help = Get-Help "dnvm-$Command" -ShowWindow:$false
        }
        if($PassThru -Or $Host.Version.Major -lt 3) {
            $help
        } else {
            _WriteOut -ForegroundColor $ColorScheme.Help_Header "$CommandName $Command"
            _WriteOut "  $($help.Synopsis.Trim())"
            _WriteOut
            _WriteOut -ForegroundColor $ColorScheme.Help_Header "usage:"
            $help.Syntax.syntaxItem | ForEach-Object {
                _WriteOut -NoNewLine -ForegroundColor $ColorScheme.Help_Executable "  $CommandName "
                _WriteOut -NoNewLine -ForegroundColor $ColorScheme.Help_Command "$Command"
                if($_.parameter) {
                    $_.parameter | ForEach-Object {
                        $cmdParam = $cmd.Parameters[$_.name]
                        $name = $_.name
                        if($cmdParam.Aliases.Length -gt 0) {
                            $name = $cmdParam.Aliases | Sort-Object | Select-Object -First 1
                        }

                        _WriteOut -NoNewLine " "

                        if($_.required -ne "true") {
                            _WriteOut -NoNewLine -ForegroundColor $ColorScheme.Help_Optional "["
                        }

                        if($_.position -eq "Named") {
                            _WriteOut -NoNewLine -ForegroundColor $ColorScheme.Help_Switch "-$name"
                        }
                        if($_.parameterValue) {
                            if($_.position -eq "Named") {
                                _WriteOut -NoNewLine " "
                            }
                            _WriteOut -NoNewLine -ForegroundColor $ColorScheme.Help_Argument "<$($_.name)>"
                        }

                        if($_.required -ne "true") {
                            _WriteOut -NoNewLine -ForegroundColor $ColorScheme.Help_Optional "]"
                        }
                    }
                }
                _WriteOut
            }

            if($help.parameters -and $help.parameters.parameter) {
                _WriteOut
                _WriteOut -ForegroundColor $ColorScheme.Help_Header "options:"
                $help.parameters.parameter | ForEach-Object {
                    $cmdParam = $cmd.Parameters[$_.name]
                    $name = $_.name
                    if($cmdParam.Aliases.Length -gt 0) {
                        $name = $cmdParam.Aliases | Sort-Object | Select-Object -First 1
                    }

                    _WriteOut -NoNewLine "  "

                    if($_.position -eq "Named") {
                        _WriteOut -NoNewLine -ForegroundColor $ColorScheme.Help_Switch "-$name".PadRight($OptionPadding)
                    } else {
                        _WriteOut -NoNewLine -ForegroundColor $ColorScheme.Help_Argument "<$($_.name)>".PadRight($OptionPadding)
                    }
                    _WriteOut " $($_.description.Text)"
                }
            }

            if($help.description) {
                _WriteOut
                _WriteOut -ForegroundColor $ColorScheme.Help_Header "remarks:"
                $help.description.Text.Split(@("`r", "`n"), "RemoveEmptyEntries") |
                    ForEach-Object { _WriteOut "  $_" }
            }

            if($DeprecatedCommands -contains $Command) {
                _WriteOut "This command has been deprecated and should not longer be used"
            }
        }
    } else {
        Write-Usage
        Write-Feeds
        _WriteOut
        _WriteOut -ForegroundColor $ColorScheme.Help_Header "commands: "
        Get-Command "$CommandPrefix*" |
            ForEach-Object {
                if($Host.Version.Major -lt 3) {
                    $h = Get-Help $_.Name
                } else {
                    $h = Get-Help $_.Name -ShowWindow:$false
                }
                $name = $_.Name.Substring($CommandPrefix.Length)
                if($DeprecatedCommands -notcontains $name) {
                    _WriteOut -NoNewLine "    "
                    _WriteOut -NoNewLine -ForegroundColor $ColorScheme.Help_Command $name.PadRight($CommandPadding)
                    _WriteOut " $($h.Synopsis.Trim())"
                }
            }
    }
}

filter ColorActive {
    param([string] $color)
    $lines = $_.Split("`n")
    foreach($line in $lines) {
        if($line.Contains("*")){
            _WriteOut -ForegroundColor $ColorScheme.ActiveRuntime $line
        } else {
            _WriteOut $line
        }
    }
}

<#
.SYNOPSIS
    Displays the DNVM version.
#>
function dnvm-version {
    _WriteOut "$FullVersion"
}

<#
.SYNOPSIS
    Lists available runtimes
.PARAMETER Detailed
    Display more detailed information on each runtime
.PARAMETER PassThru
    Set this switch to return unformatted powershell objects for use in scripting
#>
function dnvm-list {
    param(
        [Parameter(Mandatory=$false)][switch]$PassThru,
        [Parameter(Mandatory=$false)][switch]$Detailed)
    $aliases = Get-RuntimeAlias

    if(-not $PassThru) {
        Check-Runtimes
    }

    $items = @()
    $RuntimeHomes | ForEach-Object {
        _WriteDebug "Scanning $_ for runtimes..."
        if (Test-Path "$_\runtimes") {
            $items += Get-ChildItem "$_\runtimes\$RuntimePackageName-*" | List-Parts $aliases $items
        }
    }

    $aliases | Where-Object {$_.Orphan} | ForEach-Object {
        $items += $_ | Select-Object @{label='Name';expression={$_.Name}}, @{label='FullName';expression={Join-Path $RuntimesDir $_.Name}} | List-Parts $aliases
    }

    if($PassThru) {
        $items
    } else {
        if($items) {
            #TODO: Probably a better way to do this.
            if($Detailed) {
                $items |
                    Sort-Object Version, Runtime, Architecture, OperatingSystem, Alias |
                    Format-Table -AutoSize -Property @{name="Active";expression={if($_.Active) { "*" } else { "" }};alignment="center"}, "Version", "Runtime", "Architecture", "OperatingSystem", "Alias", "Location" | Out-String| ColorActive
            } else {
                $items |
                    Sort-Object Version, Runtime, Architecture, OperatingSystem, Alias |
                    Format-Table -AutoSize -Property @{name="Active";expression={if($_.Active) { "*" } else { "" }};alignment="center"}, "Version", "Runtime", "Architecture", "OperatingSystem", "Alias" | Out-String | ColorActive
            }
        } else {
            _WriteOut "No runtimes installed. You can run `dnvm install latest` or `dnvm upgrade` to install a runtime."
        }
    }
}

<#
.SYNOPSIS
    Lists and manages aliases
.PARAMETER Name
    The name of the alias to read/create/delete
.PARAMETER Version
    The version to assign to the new alias
.PARAMETER Architecture
    The architecture of the runtime to assign to this alias
.PARAMETER Runtime
    The flavor of the runtime to assign to this alias
.PARAMETER OS
    The operating system that the runtime targets
.PARAMETER Delete
    Set this switch to delete the alias with the specified name
.DESCRIPTION
    If no arguments are provided, this command lists all aliases. If <Name> is provided,
    the value of that alias, if present, is displayed. If <Name> and <Version> are
    provided, the alias <Name> is set to the runtime defined by <Version>, <Architecture>
    (defaults to 'x86') and <Runtime> (defaults to 'clr').

    Finally, if the '-d' switch is provided, the alias <Name> is deleted, if it exists.

    NOTE: You cannot create an alias for a non-windows runtime. The intended use case for
    an alias to help make it easier to switch the runtime, and you cannot use a non-windows
    runtime on a windows machine.
#>
function dnvm-alias {
    param(
        [Alias("d")]
        [switch]$Delete,

        [Parameter(Position=0)]
        [string]$Name,

        [Parameter(Position=1)]
        [string]$Version,

        [Alias("arch", "a")]
        [ValidateSet("", "x86", "x64", "arm")]
        [string]$Architecture = "",

        [Alias("r")]
        [ValidateSet("", "clr", "coreclr", "mono")]
        [Parameter(ParameterSetName="Write")]
        [string]$Runtime = "",

        [ValidateSet("win", "osx", "darwin", "linux")]
        [Parameter(Mandatory=$false,ParameterSetName="Write")]
        [string]$OS = "")

    if($Name -like "help" -or $Name -like "/?") {
        #It is unlikely that the user is trying to read an alias called help, so lets just help them out by displaying help text.
        #If people need an alias called help or one that contains a `?` then we can change this to a prompt.
        dnvm help alias
        return
    }

    if($Version) {
        Write-Alias $Name $Version -Architecture $Architecture -Runtime $Runtime -OS:$OS
    } elseif ($Delete) {
        Delete-Alias $Name
    } else {
        Read-Alias $Name
    }
}

<#
.SYNOPSIS
    [DEPRECATED] Removes an alias
.PARAMETER Name
    The name of the alias to remove
#>
function dnvm-unalias {
    param(
        [Parameter(Mandatory=$true,Position=0)][string]$Name)
    _WriteOut "This command has been deprecated. Use '$CommandName alias -d' instead"
    dnvm-alias -Delete -Name $Name
}

<#
.SYNOPSIS
    Installs the latest version of the runtime and reassigns the specified alias to point at it
.PARAMETER Alias
    The alias to upgrade (default: 'default')
.PARAMETER Architecture
    The processor architecture of the runtime to install (default: x86)
.PARAMETER Runtime
    The runtime flavor to install (default: clr)
.PARAMETER OS
    The operating system that the runtime targets (default: win)
.PARAMETER Force
    Overwrite an existing runtime if it already exists
.PARAMETER Proxy
    Use the given address as a proxy when accessing remote server
.PARAMETER NoNative
    Skip generation of native images
.PARAMETER Ngen
    For CLR flavor only. Generate native images for runtime libraries on Desktop CLR to improve startup time. This option requires elevated privilege and will be automatically turned on if the script is running in administrative mode. To opt-out in administrative mode, use -NoNative switch.
.PARAMETER Unstable
    Upgrade from the unstable dev feed. This will give you the latest development version of the runtime.
.PARAMETER Global
    Installs to configured global dnx file location (default: C:\ProgramData)
#>
function dnvm-upgrade {
    param(
        [Parameter(Mandatory=$false, Position=0)]
        [string]$Alias = "default",

        [Alias("arch", "a")]
        [ValidateSet("", "x86", "x64", "arm")]
        [Parameter(Mandatory=$false)]
        [string]$Architecture = "",

        [Alias("r")]
        [ValidateSet("", "clr", "coreclr", "mono")]
        [Parameter(Mandatory=$false)]
        [string]$Runtime = "",

        [ValidateSet("", "win", "osx", "darwin", "linux")]
        [Parameter(Mandatory=$false)]
        [string]$OS = "",

        [Alias("f")]
        [Parameter(Mandatory=$false)]
        [switch]$Force,

        [Parameter(Mandatory=$false)]
        [string]$Proxy,

        [Parameter(Mandatory=$false)]
        [switch]$NoNative,

        [Parameter(Mandatory=$false)]
        [switch]$Ngen,

        [Alias("u")]
        [Parameter(Mandatory=$false)]
        [switch]$Unstable,

        [Alias("g")]
        [Parameter(Mandatory=$false)]
        [switch]$Global)

    if($OS -ne "win" -and ![String]::IsNullOrEmpty($OS)) {
        #We could remove OS as an option from upgrade, but I want to take this opporunty to educate users about the difference between install and upgrade
        #It's possible we should just do install here instead.
         _WriteOut -ForegroundColor $ColorScheme.Error "You cannot upgrade to a non-windows runtime. Upgrade will download the latest version of the $RuntimeShortFriendlyName and also set it as your machines default. You cannot set the default $RuntimeShortFriendlyName to a non-windows version because you cannot use it to run an application. If you want to install a non-windows $RuntimeShortFriendlyName to package with your application then use 'dnvm install latest -OS:$OS' instead. Install will download the package but not set it as your default."
        $Script:ExitCode = $ExitCodes.OtherError
        return
    }

    dnvm-install "latest" -Alias:$Alias -Architecture:$Architecture -Runtime:$Runtime -OS:$OS -Force:$Force -Proxy:$Proxy -NoNative:$NoNative -Ngen:$Ngen -Unstable:$Unstable -Persistent:$true -Global:$Global
}

<#
.SYNOPSIS
    Installs a version of the runtime
.PARAMETER VersionNuPkgOrAlias
    The version to install from the current channel, the path to a '.nupkg' file to install, 'latest' to
    install the latest available version from the current channel, or an alias value to install an alternate
    runtime or architecture flavor of the specified alias.
.PARAMETER Architecture
    The processor architecture of the runtime to install (default: x86)
.PARAMETER Runtime
    The runtime flavor to install (default: clr)
.PARAMETER OS
    The operating system that the runtime targets (default: win)
.PARAMETER Alias
    Set alias <Alias> to the installed runtime
.PARAMETER Force
    Overwrite an existing runtime if it already exists
.PARAMETER Proxy
    Use the given address as a proxy when accessing remote server
.PARAMETER NoNative
    Skip generation of native images
.PARAMETER Ngen
    For CLR flavor only. Generate native images for runtime libraries on Desktop CLR to improve startup time. This option requires elevated privilege and will be automatically turned on if the script is running in administrative mode. To opt-out in administrative mode, use -NoNative switch.
.PARAMETER Persistent
    Make the installed runtime useable across all processes run by the current user
.PARAMETER Unstable
    Upgrade from the unstable dev feed. This will give you the latest development version of the runtime.
.PARAMETER Global
    Installs to configured global dnx file location (default: C:\ProgramData)
.DESCRIPTION
    A proxy can also be specified by using the 'http_proxy' environment variable
#>
function dnvm-install {
    param(
        [Parameter(Mandatory=$false, Position=0)]
        [string]$VersionNuPkgOrAlias,

        [Alias("arch", "a")]
        [ValidateSet("", "x86", "x64", "arm")]
        [Parameter(Mandatory=$false)]
        [string]$Architecture = "",

        [Alias("r")]
        [ValidateSet("", "clr", "coreclr", "mono")]
        [Parameter(Mandatory=$false)]
        [string]$Runtime = "",

        [ValidateSet("", "win", "osx", "darwin", "linux")]
        [Parameter(Mandatory=$false)]
        [string]$OS = "",

        [Parameter(Mandatory=$false)]
        [string]$Alias,

        [Alias("f")]
        [Parameter(Mandatory=$false)]
        [switch]$Force,

        [Parameter(Mandatory=$false)]
        [string]$Proxy,

        [Parameter(Mandatory=$false)]
        [switch]$NoNative,

        [Parameter(Mandatory=$false)]
        [switch]$Ngen,

        [Alias("p")]
        [Parameter(Mandatory=$false)]
        [switch]$Persistent,

        [Alias("u")]
        [Parameter(Mandatory=$false)]
        [switch]$Unstable,

        [Alias("g")]
        [Parameter(Mandatory=$false)]
        [switch]$Global)

    $selectedFeed = ""

    if($Unstable) {
        $selectedFeed = $ActiveUnstableFeed
        if(!$selectedFeed) {
            $selectedFeed = $DefaultUnstableFeed
        } else {
            _WriteOut -ForegroundColor $ColorScheme.Warning "Default unstable feed ($DefaultUnstableFeed) is being overridden by the value of the $DefaultUnstableFeedKey environment variable ($ActiveUnstableFeed)"
        }
    } else {
        $selectedFeed = $ActiveFeed
        if(!$selectedFeed) {
            $selectedFeed = $DefaultFeed
        } else {
            _WriteOut -ForegroundColor $ColorScheme.Warning "Default stable feed ($DefaultFeed) is being overridden by the value of the $DefaultFeedKey environment variable ($ActiveFeed)"
        }
    }

    if(!$VersionNuPkgOrAlias) {
        _WriteOut "A version, nupkg path, or the string 'latest' must be provided."
        dnvm-help install
        $Script:ExitCode = $ExitCodes.InvalidArguments
        return
    }

    $IsNuPkg = $VersionNuPkgOrAlias.EndsWith(".nupkg")

    if ($IsNuPkg) {
        if(!(Test-Path $VersionNuPkgOrAlias)) {
            throw "Unable to locate package file: '$VersionNuPkgOrAlias'"
        }
        Write-Progress -Activity "Installing runtime" -Status "Parsing package file name" -Id 1
        $runtimeFullName = [System.IO.Path]::GetFileNameWithoutExtension($VersionNuPkgOrAlias)
        $Architecture = Get-PackageArch $runtimeFullName
        $Runtime = Get-PackageRuntime $runtimeFullName
        $OS = Get-PackageOS $runtimeFullName
        $Version = Get-PackageVersion $runtimeFullName
    } else {
        $aliasPath = Join-Path $AliasesDir "$VersionNuPkgOrAlias$AliasExtension"
        if(Test-Path $aliasPath) {
            $BaseName = Get-Content $aliasPath
            #Check empty checks let us override a given alias property when installing the same again. e.g. `dnvm install default -x64`
            if([String]::IsNullOrEmpty($Architecture)) {
                $Architecture = Get-PackageArch $BaseName
            }

            if([String]::IsNullOrEmpty($Runtime)) {
                $Runtime = Get-PackageRuntime $BaseName
            }

            if([String]::IsNullOrEmpty($Version)) {
                $Version = Get-PackageVersion $BaseName
            }
 
            if([String]::IsNullOrEmpty($OS)) {
                $OS = Get-PackageOS $BaseName
            }
        } else {
            $Version = $VersionNuPkgOrAlias
        }
    }

    $runtimeInfo = GetRuntimeInfo $Architecture $Runtime $OS $Version

    if (!$IsNuPkg) {
        if ($VersionNuPkgOrAlias -eq "latest") {
            Write-Progress -Activity "Installing runtime" -Status "Determining latest runtime" -Id 1
            $findPackageResult = Find-Latest -runtimeInfo:$runtimeInfo -Feed:$selectedFeed -Proxy:$Proxy
        }
        else {
            $findPackageResult = Find-Package -runtimeInfo:$runtimeInfo -Feed:$selectedFeed -Proxy:$Proxy
        }
        $Version = $findPackageResult.Version
    }

    #If the version is still empty at this point then VersionOrNupkgOrAlias is an actual version.
    if([String]::IsNullOrEmpty($Version)) {
        $Version = $VersionNuPkgOrAlias
    }

    $runtimeInfo.Version = $Version

    _WriteDebug "Preparing to install runtime '$($runtimeInfo.RuntimeName)'"
    _WriteDebug "Architecture: $($runtimeInfo.Architecture)"
    _WriteDebug "Runtime: $($runtimeInfo.Runtime)"
    _WriteDebug "Version: $($runtimeInfo.Version)"
    _WriteDebug "OS: $($runtimeInfo.OS)"

    $installDir = $RuntimesDir
    if (!$Global) {
        $RuntimeFolder = Join-Path $RuntimesDir $($runtimeInfo.RuntimeName)
    }
    else {
        $installDir = $GlobalRuntimesDir
        $RuntimeFolder = Join-Path $GlobalRuntimesDir $($runtimeInfo.RuntimeName)
    }

    _WriteDebug "Destination: $RuntimeFolder"

    if((Test-Path $RuntimeFolder) -and $Force) {
        _WriteOut "Cleaning existing installation..."
        Remove-Item $RuntimeFolder -Recurse -Force
    }

    $installed=""
    if(Test-Path (Join-Path $RuntimesDir $($runtimeInfo.RuntimeName))) {
        $installed = Join-Path $RuntimesDir $($runtimeInfo.RuntimeName)
    }
    if(Test-Path (Join-Path $GlobalRuntimesDir $($runtimeInfo.RuntimeName))) {
        $installed = Join-Path $GlobalRuntimesDir $($runtimeInfo.RuntimeName)
    }
    if($installed -ne "") {
        _WriteOut "'$($runtimeInfo.RuntimeName)' is already installed in $installed."
        if($runtimeInfo.OS -eq "win") {
            dnvm-use $runtimeInfo.Version -Architecture:$runtimeInfo.Architecture -Runtime:$runtimeInfo.Runtime -Persistent:$Persistent -OS:$runtimeInfo.OS
        }
    }
    else {

        $Architecture = $runtimeInfo.Architecture
        $Runtime = $runtimeInfo.Runtime
        $OS = $runtimeInfo.OS

        $TempFolder = Join-Path $installDir "temp"
        $UnpackFolder = Join-Path $TempFolder $runtimeFullName
        $DownloadFile = Join-Path $UnpackFolder "$runtimeFullName.nupkg"

        if(Test-Path $UnpackFolder) {
            _WriteDebug "Cleaning temporary directory $UnpackFolder"
            Remove-Item $UnpackFolder -Recurse -Force
        }
        New-Item -Type Directory $UnpackFolder | Out-Null

        if($IsNuPkg) {
            Write-Progress -Activity "Installing runtime" -Status "Copying package" -Id 1
            _WriteDebug "Copying local nupkg $VersionNuPkgOrAlias to $DownloadFile"
            Copy-Item $VersionNuPkgOrAlias $DownloadFile
        } else {
            # Download the package
            Write-Progress -Activity "Installing runtime" -Status "Downloading runtime" -Id 1
            _WriteDebug "Downloading version $($runtimeInfo.Version) to $DownloadFile"

            Download-Package -RuntimeInfo:$runtimeInfo -DownloadUrl:$findPackageResult.DownloadUrl -DestinationFile:$DownloadFile -Proxy:$Proxy -Feed:$selectedFeed
        }

        Write-Progress -Activity "Installing runtime" -Status "Unpacking runtime" -Id 1
        Unpack-Package $DownloadFile $UnpackFolder

        if(Test-Path $RuntimeFolder) {
            # Ensure the runtime hasn't been installed in the time it took to download the package.
            _WriteOut "'$($runtimeInfo.RuntimeName)' is already installed."
        }
        else {
            _WriteOut "Installing to $RuntimeFolder"
            _WriteDebug "Moving package contents to $RuntimeFolder"
            $retry=0
            while($retry -ne 2) {
                try {
                    Move-Item $UnpackFolder $RuntimeFolder -Force
                    break
                } catch {
                    $retry=$retry+1
                    if($retry -eq 2) {
                        if(Test-Path $RuntimeFolder) {
                            #Attempt to cleanup the runtime folder if it is there after a fail.
                            _WriteDebug "Deleting $RuntimeFolder"
                            Remove-Item $RuntimeFolder -Recurse -Force
                            throw
                        }
                    }
                }
            }
            #If there is nothing left in the temp folder remove it. There could be other installs happening at the same time as this.
            if(Test-Path $(Join-Path $TempFolder "*")) {
                Remove-Item $TempFolder -Recurse
            }
        }

        if($runtimeInfo.OS -eq "win") {
            dnvm-use $runtimeInfo.Version -Architecture:$runtimeInfo.Architecture -Runtime:$runtimeInfo.Runtime -Persistent:$Persistent -OS:$runtimeInfo.OS
        }

        if ($runtimeInfo.Runtime -eq "clr") {
            if (-not $NoNative) {
                if ((Is-Elevated) -or $Ngen) {
                    $runtimeBin = Get-RuntimePath $runtimeInfo.RuntimeName
                    Write-Progress -Activity "Installing runtime" -Status "Generating runtime native images" -Id 1
                    Ngen-Library $runtimeBin $runtimeInfo.Architecture
                }
                else {
                    _WriteOut "Native image generation (ngen) is skipped. Include -Ngen switch to turn on native image generation to improve application startup time."
                }
            }
        }
        elseif ($runtimeInfo.Runtime -eq "coreclr") {
            if ($NoNative -or $runtimeInfo.OS -ne "win") {
                _WriteOut "Skipping native image compilation."
            }
            else {
                _WriteOut "Compiling native images for $($runtimeInfo.RuntimeName) to improve startup performance..."
                Write-Progress -Activity "Installing runtime" -Status "Generating runtime native images" -Id 1

                if(Get-Command $CrossGenCommand -ErrorAction SilentlyContinue) {
                    $crossGenCommand = $CrossGenCommand
                } else {
                    $crossGenCommand = $OldCrossGenCommand
                }

                if ($DebugPreference -eq 'SilentlyContinue') {
                    Start-Process $crossGenCommand -Wait -WindowStyle Hidden
                }
                else {
                    Start-Process $crossGenCommand -Wait -NoNewWindow
                }
                _WriteOut "Finished native image compilation."
            }
        }
        else {
            _WriteOut "Unexpected platform: $($runtimeInfo.Runtime). No optimization would be performed on the package installed."
        }
    }

    if($Alias) {
        if($runtimeInfo.OS -eq "win") {
            _WriteDebug "Aliasing installed runtime to '$Alias'"
            dnvm-alias $Alias $runtimeInfo.Version -Architecture:$RuntimeInfo.Architecture -Runtime:$RuntimeInfo.Runtime -OS:$RuntimeInfo.OS
        } else {
            _WriteOut "Unable to set an alias for a non-windows runtime. Installing non-windows runtimes on Windows are meant only for publishing, not running."
        }
    }

    Write-Progress -Status "Done" -Activity "Install complete" -Id 1 -Complete
}

<#
.SYNOPSIS
    Uninstalls a version of the runtime
.PARAMETER VersionOrAlias
    The version to uninstall from the current channel or an alias value to uninstall an alternate
    runtime or architecture flavor of the specified alias.
.PARAMETER Architecture
    The processor architecture of the runtime to uninstall (default: x86)
.PARAMETER Runtime
    The runtime flavor to uninstall (default: clr)
.PARAMETER OS
    The operating system that the runtime targets (default: win)
#>
function dnvm-uninstall {
    param(
        [Parameter(Mandatory=$true, Position=0)]
        [string]$VersionOrAlias,

        [Alias("arch", "a")]
        [ValidateSet("", "x86", "x64", "arm")]
        [Parameter(Mandatory=$false)]
        [string]$Architecture = "",

        [Alias("r")]
        [ValidateSet("", "clr", "coreclr", "mono")]
        [Parameter(Mandatory=$false)]
        [string]$Runtime = "",

        [ValidateSet("", "win", "osx", "darwin", "linux")]
        [Parameter(Mandatory=$false)]
        [string]$OS = "")

    $aliasPath = Join-Path $AliasesDir "$VersionOrAlias$AliasExtension"
    
    if(Test-Path $aliasPath) {
        $BaseName = Get-Content $aliasPath
    } else {
        $Version = $VersionOrAlias
        $runtimeInfo = GetRuntimeInfo $Architecture $Runtime $OS $Version
        $BaseName = $runtimeInfo.RuntimeName
    }

    $runtimeFolder=""
    if(Test-Path (Join-Path $RuntimesDir $BaseName)) {
        $runtimeFolder = Join-Path $RuntimesDir $BaseName
    }
    if(Test-Path (Join-Path $GlobalRuntimesDir $BaseName)) {
        $runtimeFolder = Join-Path $GlobalRuntimesDir $BaseName
    }

    if($runtimeFolder -ne "") {
        Remove-Item -literalPath $runtimeFolder -Force -Recurse
        _WriteOut "Removed '$($runtimeFolder)'"
    } else {
        _WriteOut "'$($BaseName)' is not installed"
    }

    $aliases = Get-RuntimeAlias

    $result = @($aliases | Where-Object { $_.Name.EndsWith($BaseName) })
    foreach($alias in $result) {
        dnvm-alias -Delete -Name $alias.Alias
    }
}

<#
.SYNOPSIS
    Adds a runtime to the PATH environment variable for your current shell
.PARAMETER VersionOrAlias
    The version or alias of the runtime to place on the PATH
.PARAMETER Architecture
    The processor architecture of the runtime to place on the PATH (default: x86, or whatever the alias specifies in the case of use-ing an alias)
.PARAMETER Runtime
    The runtime flavor of the runtime to place on the PATH (default: clr, or whatever the alias specifies in the case of use-ing an alias)
.PARAMETER OS
    The operating system that the runtime targets (default: win)
.PARAMETER Persistent
    Make the change persistent across all processes run by the current user
#>
function dnvm-use {
    param(
        [Parameter(Mandatory=$true, Position=0)]
        [string]$VersionOrAlias,

        [Alias("arch", "a")]
        [ValidateSet("", "x86", "x64", "arm")]
        [Parameter(Mandatory=$false)]
        [string]$Architecture = "",

        [Alias("r")]
        [ValidateSet("", "clr", "coreclr")]
        [Parameter(Mandatory=$false)]
        [string]$Runtime = "",

        [ValidateSet("", "win", "osx", "darwin", "linux")]
        [Parameter(Mandatory=$false)]
        [string]$OS = "",

        [Alias("p")]
        [Parameter(Mandatory=$false)]
        [switch]$Persistent)

    if ($versionOrAlias -eq "none") {
        _WriteOut "Removing all runtimes from process PATH"
        Set-Path (Change-Path $env:Path "" $RuntimeDirs)

        if ($Persistent) {
            _WriteOut "Removing all runtimes from user PATH"
            $userPath = [Environment]::GetEnvironmentVariable("Path", [System.EnvironmentVariableTarget]::User)
            $userPath = Change-Path $userPath "" $RuntimeDirs
            [Environment]::SetEnvironmentVariable("Path", $userPath, [System.EnvironmentVariableTarget]::User)
        }
        return;
    }

    $runtimeInfo = Get-RuntimeAliasOrRuntimeInfo -Version:$VersionOrAlias -Architecture:$Architecture -Runtime:$Runtime -OS:$OS
    $runtimeFullName = $runtimeInfo.RuntimeName
    $runtimeBin = Get-RuntimePath $runtimeFullName
    if ($runtimeBin -eq $null) {
        throw "Cannot find $runtimeFullName, do you need to run '$CommandName install $versionOrAlias'?"
    }

    _WriteOut "Adding $runtimeBin to process PATH"
    Set-Path (Change-Path $env:Path $runtimeBin $RuntimeDirs)

    if ($Persistent) {
        _WriteOut "Adding $runtimeBin to user PATH"
        $userPath = [Environment]::GetEnvironmentVariable("Path", [System.EnvironmentVariableTarget]::User)
        $userPath = Change-Path $userPath $runtimeBin $RuntimeDirs
        [Environment]::SetEnvironmentVariable("Path", $userPath, [System.EnvironmentVariableTarget]::User)
    }
}

<#
.SYNOPSIS
    Locates the dnx.exe for the specified version or alias and executes it, providing the remaining arguments to dnx.exe
.PARAMETER VersionOrAlias
    The version of alias of the runtime to execute
.PARAMETER Architecture
    The processor architecture of the runtime to use (default: x86, or whatever the alias specifies in the case of running an alias)
.PARAMETER Runtime
    The runtime flavor of the runtime to use (default: clr, or whatever the alias specifies in the case of running an alias)
.PARAMETER DnxArguments
    The arguments to pass to dnx.exe
#>
function dnvm-run {
    param(
        [Parameter(Mandatory=$true, Position=0)]
        [string]$VersionOrAlias,

        [Alias("arch", "a")]
        [ValidateSet("", "x86", "x64", "arm")]
        [Parameter(Mandatory=$false)]
        [string]$Architecture = "",

        [Alias("r")]
        [ValidateSet("", "clr", "coreclr")]
        [Parameter(Mandatory=$false)]
        [string]$Runtime = "",

        [Parameter(Mandatory=$false, Position=1, ValueFromRemainingArguments=$true)]
        [object[]]$DnxArguments)

    $runtimeInfo = Get-RuntimeAliasOrRuntimeInfo -Version:$VersionOrAlias -Runtime:$Runtime -Architecture:$Architecture

    $runtimeBin = Get-RuntimePath $runtimeInfo.RuntimeName
    if ($runtimeBin -eq $null) {
        throw "Cannot find $($runtimeInfo.Name), do you need to run '$CommandName install $versionOrAlias'?"
    }
    $dnxExe = Join-Path $runtimeBin "dnx.exe"
    if(!(Test-Path $dnxExe)) {
        throw "Cannot find a dnx.exe in $runtimeBin, the installation may be corrupt. Try running 'dnvm install $VersionOrAlias -f' to reinstall it"
    }
    _WriteDebug "> $dnxExe $DnxArguments"
    & $dnxExe @DnxArguments
    $Script:ExitCode = $LASTEXITCODE
}

<#
.SYNOPSIS
    Executes the specified command in a sub-shell where the PATH has been augmented to include the specified DNX
.PARAMETER VersionOrAlias
    The version of alias of the runtime to make active in the sub-shell
.PARAMETER Architecture
    The processor architecture of the runtime to use (default: x86, or whatever the alias specifies in the case of exec-ing an alias)
.PARAMETER Runtime
    The runtime flavor of the runtime to use (default: clr, or whatever the alias specifies in the case of exec-ing an alias)
.PARAMETER Command
    The command to execute in the sub-shell
#>
function dnvm-exec {
    param(
        [Parameter(Mandatory=$true, Position=0)]
        [string]$VersionOrAlias,
        [Parameter(Mandatory=$false, Position=1)]
        [string]$Command,

        [Alias("arch", "a")]
        [ValidateSet("", "x86", "x64", "arm")]
        [Parameter(Mandatory=$false)]
        [string]$Architecture = "",

        [Alias("r")]
        [ValidateSet("", "clr", "coreclr")]
        [Parameter(Mandatory=$false)]
        [string]$Runtime = "",
        [Parameter(Mandatory=$false, Position=2, ValueFromRemainingArguments=$true)]
        [object[]]$Arguments)

    $runtimeInfo = Get-RuntimeAliasOrRuntimeInfo -Version:$VersionOrAlias -Runtime:$Runtime -Architecture:$Architecture
    $runtimeBin = Get-RuntimePath $runtimeInfo.RuntimeName

    if ($runtimeBin -eq $null) {
        throw "Cannot find $($runtimeInfo.RuntimeName), do you need to run '$CommandName install $versionOrAlias'?"
    }

    $oldPath = $env:PATH
    try {
        $env:PATH = "$runtimeBin;$($env:PATH)"
        & $Command @Arguments
    } finally {
        $Script:ExitCode = $LASTEXITCODE
        $env:PATH = $oldPath
    }
}

<#
.SYNOPSIS
    Installs the version manager into your User profile directory
.PARAMETER SkipUserEnvironmentInstall
    Set this switch to skip configuring the user-level DNX_HOME and PATH environment variables
#>
function dnvm-setup {
    param(
        [switch]$SkipUserEnvironmentInstall)

    $DestinationHome = [Environment]::ExpandEnvironmentVariables("$DefaultUserHome")

    # Install scripts
    $Destination = "$DestinationHome\bin"
    _WriteOut "Installing $CommandFriendlyName to $Destination"

    $ScriptFolder = Split-Path -Parent $ScriptPath

    # Copy script files (if necessary):
    Safe-Filecopy "$CommandName.ps1" $ScriptFolder $Destination
    Safe-Filecopy "$CommandName.cmd" $ScriptFolder $Destination

    # Configure Environment Variables
    # Also, clean old user home values if present
    # We'll be removing any existing homes, both
    $PathsToRemove = @(
        "$DefaultUserHome",
        [Environment]::ExpandEnvironmentVariables($OldUserHome),
        $DestinationHome,
        $OldUserHome)

    # First: PATH
    _WriteOut "Adding $Destination to Process PATH"
    Set-Path (Change-Path $env:PATH $Destination $PathsToRemove)

    if(!$SkipUserEnvironmentInstall) {
        _WriteOut "Adding $Destination to User PATH"
        $userPath = [Environment]::GetEnvironmentVariable("PATH", "User")
        $userPath = Change-Path $userPath $Destination $PathsToRemove
        [Environment]::SetEnvironmentVariable("PATH", $userPath, "User")
    }

    # Now clean up the HomeEnvVar if currently set; script installed to default location.
    Clean-HomeEnv($SkipUserEnvironmentInstall)
}

function Check-Runtimes(){
    $runtimesInstall = $false;
    foreach($runtimeHomeDir in $RuntimeHomes) {
        if (Test-Path "$runtimeHomeDir\runtimes") {
            if(Test-Path "$runtimeHomeDir\runtimes\$RuntimePackageName-*"){
                $runtimesInstall = $true;
                break;
            }
        }
    }

    if (-not $runtimesInstall){
        $title = "Getting started"
        $message = "It looks like you don't have any runtimes installed. Do you want us to install a $RuntimeShortFriendlyName to get you started?"

        $yes = New-Object System.Management.Automation.Host.ChoiceDescription "&Yes", "Install the latest runtime for you"

        $no = New-Object System.Management.Automation.Host.ChoiceDescription "&No", "Do not install the latest runtime and continue"

        $options = [System.Management.Automation.Host.ChoiceDescription[]]($yes, $no)

        $result = $host.ui.PromptForChoice($title, $message, $options, 0)

        if($result -eq 0){
            dnvm-upgrade
        }
    }
}

### The main "entry point"

# Check for old DNX_HOME values
if($UnencodedHomes -contains $OldUserHome) {
    _WriteOut -ForegroundColor Yellow "WARNING: Found '$OldUserHome' in your $HomeEnvVar value. This folder has been deprecated."
    if($UnencodedHomes -notcontains $DefaultUserHome) {
        _WriteOut -ForegroundColor Yellow "WARNING: Didn't find '$DefaultUserHome' in your $HomeEnvVar value. You should run '$CommandName setup' to upgrade."
    }
}

# Check for old KRE_HOME variable
if(Test-Path env:\KRE_HOME) {
    _WriteOut -ForegroundColor Yellow "WARNING: Found a KRE_HOME environment variable. This variable has been deprecated and should be removed, or it may interfere with DNVM and the .NET Execution environment"
}

# Read arguments

$cmd = $args[0]

$cmdargs = @()
if($args.Length -gt 1) {
    # Combine arguments, ensuring any containing whitespace or parenthesis are correctly quoted 
    ForEach ($arg In $args[1..($args.Length-1)]) {
        if ($arg -match "[\s\(\)]") {
            $cmdargs += """$arg"""
        } else {
            $cmdargs += $arg
        }
        $cmdargs += " "
    }
}

# Can't add this as script-level arguments because they mask '-a' arguments in subcommands!
# So we manually parse them :)
if($cmdargs -icontains "-amd64") {
    $CompatArch = "x64"
    _WriteOut "The -amd64 switch has been deprecated. Use the '-arch x64' parameter instead"
} elseif($cmdargs -icontains "-x86") {
    $CompatArch = "x86"
    _WriteOut "The -x86 switch has been deprecated. Use the '-arch x86' parameter instead"
} elseif($cmdargs -icontains "-x64") {
    $CompatArch = "x64"
    _WriteOut "The -x64 switch has been deprecated. Use the '-arch x64' parameter instead"
}
$cmdargs = @($cmdargs | Where-Object { @("-amd64", "-x86", "-x64") -notcontains $_ })

if(!$cmd) {
    Check-Runtimes
    $cmd = "help"
    $Script:ExitCode = $ExitCodes.InvalidArguments
}

# Check for the command and run it
try {
    if(Get-Command -Name "$CommandPrefix$cmd" -ErrorAction SilentlyContinue) {
        _WriteDebug "& dnvm-$cmd $cmdargs"
        Invoke-Command ([ScriptBlock]::Create("dnvm-$cmd $cmdargs"))
    }
    else {
        _WriteOut "Unknown command: '$cmd'"
        dnvm-help
        $Script:ExitCode = $ExitCodes.UnknownCommand
    }
} catch {
    throw
    if(!$Script:ExitCode) { $Script:ExitCode = $ExitCodes.OtherError }
}

_WriteDebug "=== End $CommandName (Exit Code $Script:ExitCode) ==="
_WriteDebug ""
exit $Script:ExitCode
