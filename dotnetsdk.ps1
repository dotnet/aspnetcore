param(
  [parameter(Position=0)]
  [string] $Command,
  [string] $Proxy,
  [switch] $Verbosity = $false,
  [alias("p")][switch] $Persistent = $false,
  [alias("f")][switch] $Force = $false,
  [alias("r")][string] $Runtime,
  
  [alias("arch")][string] $Architecture,
  [switch] $X86 = $false,
  [alias("amd64")][switch] $X64 = $false,

  [alias("w")][switch] $Wait = $false,
  [alias("a")]
  [string] $Alias = $null,
  [switch] $NoNative = $false,
  [parameter(Position=1, ValueFromRemainingArguments=$true)]
  [string[]]$Args=@(),
  [switch] $Quiet,
  [string] $OutputVariable,
  [switch] $AssumeElevated
)

# "Constants" (in as much as PowerShell will allow)
$RuntimePackageName = "dotnet"
$RuntimeFriendlyName = ".NET Runtime"
$RuntimeProgramFilesName = "Microsoft .NET Cross-Platform Runtime"
$RuntimeFolderName = ".dotnet"
$DefaultFeed = "https://www.myget.org/F/aspnetvnext/api/v2"
$CrossGenCommand = "k-crossgen"

$selectedArch=$null;
$defaultArch="x86"
$selectedRuntime=$null
$defaultRuntime="clr"

# Get or calculate userDotNetPath
$userDotNetPath = $env:DOTNET_USER_PATH
if(!$userDotNetPath) { $userDotNetPath = $env:USERPROFILE + "\$RuntimeFolderName" }
$userDotNetRuntimesPath = $userDotNetPath + "\runtimes"

# Get the feed from the environment variable or set it to the default value
$feed = $env:DOTNET_FEED
if (!$feed)
{
    $feed = $DefaultFeed;
}
$feed = $feed.TrimEnd("/")

# In some environments, like Azure Websites, the Write-* cmdlets don't work
$useHostOutputMethods = $true

function String-IsEmptyOrWhitespace([string]$str) {
     return [string]::IsNullOrEmpty($str) -or $str.Trim().length -eq 0
}

$scriptPath = $myInvocation.MyCommand.Definition

function DotNetSdk-Help {
@"
.NET SDK Manager - Build 10209

USAGE: dotnetsdk <command> [options]

dotnetsdk upgrade [-X86|-X64] [-r|-Runtime CLR|CoreCLR] [-g|-Global] [-f|-Force] [-Proxy <ADDRESS>] [-NoNative]
  install latest .NET Runtime from feed
  set 'default' alias to installed version
  add KRE bin to user PATH environment variable
  -g|-Global        install to machine-wide location
  -f|-Force         upgrade even if latest is already installed
  -Proxy <ADDRESS>  use given address as proxy when accessing remote server (e.g. https://username:password@proxyserver:8080/). Alternatively set proxy using http_proxy environment variable.
  -NoNative         Do not generate native images (Effective only for CoreCLR flavors)

dotnetsdk install <semver>|<alias>|<nupkg>|latest [-X86|-X64] [-r|-Runtime CLR|CoreCLR] [-a|-Alias <alias>] [-f|-Force] [-Proxy <ADDRESS>] [-NoNative]
  <semver>|<alias>  install requested .NET Runtime from feed
  <nupkg>           install requested .NET Runtime from package on local filesystem
  latest            install latest .NET Runtime from feed
  add .NET Runtime bin to path of current command line
  -p|-Persistent    add .NET Runtime bin to PATH environment variables persistently
  -a|-Alias <alias> set alias <alias> for requested .NET Runtime on install
  -f|-Force         install even if specified version is already installed
  -Proxy <ADDRESS>  use given address as proxy when accessing remote server (e.g. https://username:password@proxyserver:8080/). Alternatively set proxy using http_proxy environment variable.
  -NoNative         Do not generate native images (Effective only for CoreCLR flavors)

dotnetsdk use <semver>|<alias>|<package>|none [-X86|-X64] [-r|-Runtime CLR|CoreCLR] [-p|-Persistent]
  <semver>|<alias>|<package>  add .NET Runtime bin to path of current command line
  none                        remove .NET Runtime bin from path of current command line
  -p|-Persistent              add .NET Runtime bin to PATH environment variable across all processes run by the current user

dotnetsdk list
  list .NET Runtime versions installed

dotnetsdk alias
  list .NET Runtime aliases which have been defined

dotnetsdk alias <alias>
  display value of the specified alias

dotnetsdk alias <alias> <semver>|<alias>|<package> [-X86|-X64] [-r|-Runtime CLR|CoreCLR]
  <alias>                      the name of the alias to set
  <semver>|<alias>|<package>   the .NET Runtime version to set the alias to. Alternatively use the version of the specified alias

dotnetsdk unalias <alias>
  remove the specified alias

"@ -replace "`n","`r`n" | Console-Write
}

function DotNetSdk-Global-Setup {
  # Sets up the 'dotnetsdk' tool and adds the user-local dotnet install directory to the DOTNET_HOME variable
  # Note: We no longer do global install via this tool. The MSI handles global install of runtimes AND will set
  # the machine level DOTNET_HOME value.

  # In this configuration, the user-level path will OVERRIDE the global path because it is placed first.

  $dotnetsdkBinPath = "$userDotNetPath\bin"

  If (Needs-Elevation)
  {
    $arguments = "-ExecutionPolicy unrestricted & '$scriptPath' setup -global -wait"
    Start-Process "$psHome\powershell.exe" -Verb runAs -ArgumentList $arguments -Wait
    Console-Write "Adding $dotnetsdkBinPath to process PATH"
    Set-Path (Change-Path $env:Path $dotnetsdkBinPath ($dotnetsdkBinPath))
    Console-Write "Adding %USERPROFILE%\$RuntimeFolderName to process DOTNET_HOME"
    $envDotNetHome = $env:DOTNET_HOME
    $envDotNetHome = Change-Path $envDotNetHome "%USERPROFILE%\$RuntimeFolderName" ("%USERPROFILE%\$RuntimeFolderName")
    $env:DOTNET_HOME = $envDotNetHome
    Console-Write "Setup complete"
    break
  }

  $scriptFolder = [System.IO.Path]::GetDirectoryName($scriptPath)

  Console-Write "Copying file $dotnetsdkBinPath\dotnetsdk.ps1"
  md $dotnetsdkBinPath -Force | Out-Null
  copy "$scriptFolder\dotnetsdk.ps1" "$dotnetsdkBinPath\dotnetsdk.ps1"

  Console-Write "Copying file $dotnetsdkBinPath\dotnetsdk.cmd"
  copy "$scriptFolder\dotnetsdk.cmd" "$dotnetsdkBinPath\dotnetsdk.cmd"

  Console-Write "Adding $dotnetsdkBinPath to process PATH"
  Set-Path (Change-Path $env:Path $dotnetsdkBinPath ($dotnetsdkBinPath))

  Console-Write "Adding $dotnetsdkBinPath to user PATH"
  $userPath = [Environment]::GetEnvironmentVariable("Path", [System.EnvironmentVariableTarget]::User)
  $userPath = Change-Path $userPath $dotnetsdkBinPath ($dotnetsdkBinPath)
  [Environment]::SetEnvironmentVariable("Path", $userPath, [System.EnvironmentVariableTarget]::User)

  Console-Write "Adding %USERPROFILE%\$RuntimeFolderName to process DOTNET_HOME"
  $envDotNetHome = $env:DOTNET_HOME
  $envDotNetHome = Change-Path $envDotNetHome "%USERPROFILE%\$RuntimeFolderName" ("%USERPROFILE%\$RuntimeFolderName")
  $env:DOTNET_HOME = $envDotNetHome

  Console-Write "Adding %USERPROFILE%\$RuntimeFolderName to machine DOTNET_HOME"
  $machineDotNetHome = [Environment]::GetEnvironmentVariable("DOTNET_HOME", [System.EnvironmentVariableTarget]::Machine)
  $machineDotNetHome = Change-Path $machineDotNetHome "%USERPROFILE%\$RuntimeFolderName" ("%USERPROFILE%\$RuntimeFolderName")
  [Environment]::SetEnvironmentVariable("DOTNET_HOME", $machineDotNetHome, [System.EnvironmentVariableTarget]::Machine)
}

function DotNetSdk-Upgrade {
param(
  [boolean] $isGlobal
)
  $Persistent = $true
  $Alias="default"
  DotNetSdk-Install "latest" $isGlobal
}

function Add-Proxy-If-Specified {
param(
  [System.Net.WebClient] $wc
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

function DotNetSdk-Find-Latest {
param(
  [string] $platform,
  [string] $architecture
)
  Console-Write "Determining latest version"

  $url = "$feed/GetUpdates()?packageIds=%27$RuntimePackageName-$platform-win-$architecture%27&versions=%270.0%27&includePrerelease=true&includeAllVersions=false"

  $wc = New-Object System.Net.WebClient
  Add-Proxy-If-Specified($wc)
  Write-Verbose "Downloading $url ..."
  [xml]$xml = $wc.DownloadString($url)

  $version = Select-Xml "//d:Version" -Namespace @{d='http://schemas.microsoft.com/ado/2007/08/dataservices'} $xml

  if (String-IsEmptyOrWhitespace($version)) {
    throw "There are no runtimes for platform '$platform', architecture '$architecture' in the feed '$feed'"
  }

  return $version
}

function Do-DotNetSdk-Download {
param(
  [string] $runtimeFullName,
  [string] $runtimesFolder
)
  $parts = $runtimeFullName.Split(".", 2)

  $url = "$feed/package/" + $parts[0] + "/" + $parts[1]
  $runtimeFolder = Join-Path $runtimesFolder $runtimeFullName
  $runtimeFile = Join-Path $runtimeFolder "$runtimeFullName.nupkg"

  If (Test-Path $runtimeFolder) {
    if($Force)
    {
      rm $runtimeFolder -Recurse -Force
    } else {
      Console-Write "$runtimeFullName already installed."
      return;
    }
  }

  Console-Write "Downloading $runtimeFullName from $feed"

  #Downloading to temp location
  $runtimeTempDownload = Join-Path $runtimesFolder "temp"
  $tempDownloadFile = Join-Path $runtimeTempDownload "$runtimeFullName.nupkg"

  if(Test-Path $runtimeTempDownload) {
    del "$runtimeTempDownload\*" -recurse
  } else {
    md $runtimeTempDownload -Force | Out-Null
  }

  $wc = New-Object System.Net.WebClient
  Add-Proxy-If-Specified($wc)
  Write-Verbose "Downloading $url ..."
  $wc.DownloadFile($url, $tempDownloadFile)

  Do-DotNetSdk-Unpack $tempDownloadFile $runtimeTempDownload

  md $runtimeFolder -Force | Out-Null
  Console-Write "Installing to $runtimeFolder"
  mv "$runtimeTempDownload\*" $runtimeFolder
  Remove-Item "$runtimeTempDownload" -Force | Out-Null
}

function Do-DotNetSdk-Unpack {
param(
  [string] $runtimeFile,
  [string] $runtimeFolder
)
  Console-Write "Unpacking to $runtimeFolder"

  $compressionLib = [System.Reflection.Assembly]::LoadWithPartialName('System.IO.Compression.FileSystem')
    
  if($compressionLib -eq $null) {
      try {
          # Shell will not recognize nupkg as a zip and throw, so rename it to zip
          $runtimeZip = [System.IO.Path]::ChangeExtension($runtimeFile, "zip")
          Rename-Item $runtimeFile $runtimeZip
          # Use the shell to uncompress the nupkg
          $shell_app=new-object -com shell.application
          $zip_file = $shell_app.namespace($runtimeZip)
          $destination = $shell_app.namespace($runtimeFolder)
          $destination.Copyhere($zip_file.items(), 0x14) #0x4 = don't show UI, 0x10 = overwrite files
      }
      finally {
        # make it a nupkg again
        Rename-Item $runtimeZip $runtimeFile
      }
  } else {
      [System.IO.Compression.ZipFile]::ExtractToDirectory($runtimeFile, $runtimeFolder)
  }

  If (Test-Path ($runtimeFolder + "\[Content_Types].xml")) {
    Remove-Item ($runtimeFolder + "\[Content_Types].xml")
  }
  If (Test-Path ($runtimeFolder + "\_rels\")) {
    Remove-Item ($runtimeFolder + "\_rels\") -Force -Recurse
  }
  If (Test-Path ($runtimeFolder + "\package\")) {
    Remove-Item ($runtimeFolder + "\package\") -Force -Recurse
  }
}

function DotNetSdk-Install {
param(
  [string] $versionOrAlias,
  [boolean] $isGlobal
)
  if ($versionOrAlias -eq "latest") {
    $versionOrAlias = DotNetSdk-Find-Latest (Requested-Platform $defaultRuntime) (Requested-Architecture $defaultArch)
  }

  if ($versionOrAlias.EndsWith(".nupkg")) {
    $runtimeFullName = [System.IO.Path]::GetFileNameWithoutExtension($versionOrAlias)
  } else {
    $runtimeFullName =  Requested-VersionOrAlias $versionOrAlias
  }

  $packageFolder = $userDotNetRuntimesPath
  
  if ($versionOrAlias.EndsWith(".nupkg")) {
    Set-Variable -Name "selectedArch" -Value (Package-Arch $runtimeFullName) -Scope Script
    Set-Variable -Name "selectedRuntime" -Value (Package-Platform $runtimeFullName) -Scope Script

    $runtimeFolder = "$packageFolder\$runtimeFullName"
    $folderExists = Test-Path $runtimeFolder

    if ($folderExists -and $Force) {
      del $runtimeFolder -Recurse -Force
      $folderExists = $false;
    }

    if ($folderExists) {
      Console-Write "Target folder '$runtimeFolder' already exists"
    } else {
      $tempUnpackFolder = Join-Path $packageFolder "temp"
      $tempDownloadFile = Join-Path $tempUnpackFolder "$runtimeFullName.nupkg"

      if(Test-Path $tempUnpackFolder) {
          del "$tempUnpackFolder\*" -recurse
      } else {
          md $tempUnpackFolder -Force | Out-Null
      }
      copy $versionOrAlias $tempDownloadFile

      Do-DotNetSdk-Unpack $tempDownloadFile $tempUnpackFolder
      md $runtimeFolder -Force | Out-Null
      Console-Write "Installing to $runtimeFolder"
      mv "$tempUnpackFolder\*" $runtimeFolder
      Remove-Item "$tempUnpackFolder" -Force | Out-Null
    }

    $packageVersion = Package-Version $runtimeFullName
    
    DotNetSdk-Use $packageVersion
    if (!$(String-IsEmptyOrWhitespace($Alias))) {
        DotNetSdk-Alias-Set $Alias $packageVersion
    }
  }
  else
  {
    Do-DotNetSdk-Download $runtimeFullName $packageFolder
    DotNetSdk-Use $versionOrAlias
    if (!$(String-IsEmptyOrWhitespace($Alias))) {
        DotNetSdk-Alias-Set "$Alias" $versionOrAlias
    }
  }

  if ($runtimeFullName.Contains("CoreCLR")) {
    if ($NoNative) {
      Console-Write "Native image generation is skipped"
    }
    else {
      Console-Write "Compiling native images for $runtimeFullName to improve startup performance..."
      Start-Process $CrossGenCommand -Wait
      Console-Write "Finished native image compilation."
    }
  }
}

function DotNetSdk-List {
  $dotnetHome = $env:DOTNET_HOME
  if (!$dotnetHome) {
    $dotnetHome = "$userDotNetPath"
  }

  md ($userDotNetPath + "\alias\") -Force | Out-Null
  $aliases = Get-ChildItem ($userDotNetPath + "\alias\") | Select @{label='Alias';expression={$_.BaseName}}, @{label='Name';expression={Get-Content $_.FullName }}

  $items = @()
  foreach($portion in $dotnetHome.Split(';')) {
    $path = [System.Environment]::ExpandEnvironmentVariables($portion)
    if (Test-Path("$path\runtimes")) {
      $items += Get-ChildItem ("$path\runtimes\dotnet-*") | List-Parts $aliases
    }
  }

  $items | Sort-Object Version, Runtime, Architecture, Alias | Format-Table -AutoSize -Property @{name="Active";expression={$_.Active};alignment="center"}, "Version", "Runtime", "Architecture", "Location", "Alias"
}

filter List-Parts {
  param($aliases)

  $hasBin = Test-Path($_.FullName+"\bin")
  if (!$hasBin) {
    return
  }
  $active = $false
  foreach($portion in $env:Path.Split(';')) {
    # Append \ to the end because otherwise you might see
    # multiple active versions if the folders have the same
    # name prefix (like 1.0-beta and 1.0)
    if ($portion.StartsWith($_.FullName + "\")) {
      $active = $true
    }
  }

  $fullAlias=""
  $delim=""

  foreach($alias in $aliases){
    if($_.Name.Split('\', 2) -contains $alias.Name){
        $fullAlias += $delim + $alias.Alias
        $delim = ", "
    }
  }

  $parts1 = $_.Name.Split('.', 2)
  $parts2 = $parts1[0].Split('-', 4)
  return New-Object PSObject -Property @{
    Active = if ($active) { "*" } else { "" }
    Version = $parts1[1]
    Runtime = $parts2[1]
    OperatingSystem = $parts2[2]
    Architecture = $parts2[3]
    Location = $_.Parent.FullName
    Alias = $fullAlias
  }
}

function DotNetSdk-Use {
param(
  [string] $versionOrAlias
)
  Validate-Full-Package-Name-Arguments-Combination $versionOrAlias

  if ($versionOrAlias -eq "none") {
    Console-Write "Removing .NET Runtime from process PATH"
    Set-Path (Change-Path $env:Path "" ($userDotNetRuntimesPath))

    if ($Persistent) {
      Console-Write "Removing .NET Runtime from user PATH"
      $userPath = [Environment]::GetEnvironmentVariable("Path", [System.EnvironmentVariableTarget]::User)
      $userPath = Change-Path $userPath "" ($userDotNetRuntimesPath)
      [Environment]::SetEnvironmentVariable("Path", $userPath, [System.EnvironmentVariableTarget]::User)
    }
    return;
  }

  $runtimeFullName = Requested-VersionOrAlias $versionOrAlias

  $runtimeBin = Locate-DotNetBinFromFullName $runtimeFullName
  if ($runtimeBin -eq $null) {
    throw "Cannot find $runtimeFullName, do you need to run 'dotnetsdk install $versionOrAlias'?"
  }

  Console-Write "Adding $runtimeBin to process PATH"
  Set-Path (Change-Path $env:Path $runtimeBin ($userDotNetRuntimesPath))

  if ($Persistent) {
    Console-Write "Adding $runtimeBin to user PATH"
    $userPath = [Environment]::GetEnvironmentVariable("Path", [System.EnvironmentVariableTarget]::User)
    $userPath = Change-Path $userPath $runtimeBin ($userDotNetRuntimesPath)
    [Environment]::SetEnvironmentVariable("Path", $userPath, [System.EnvironmentVariableTarget]::User)
  }
}

function DotNetSdk-Alias-List {
  md ($userDotNetPath + "\alias\") -Force | Out-Null

  Get-ChildItem ($userDotNetPath + "\alias\") | Select @{label='Alias';expression={$_.BaseName}}, @{label='Name';expression={Get-Content $_.FullName }} | Format-Table -AutoSize
}

function DotNetSdk-Alias-Get {
param(
  [string] $name
)
  md ($userDotNetPath + "\alias\") -Force | Out-Null
  $aliasFilePath=$userDotNetPath + "\alias\" + $name + ".txt"
  if (!(Test-Path $aliasFilePath)) {
    Console-Write "Alias '$name' does not exist"
    $script:exitCode = 1 # Return non-zero exit code for scripting
  } else {
    $aliasValue = (Get-Content ($userDotNetPath + "\alias\" + $name + ".txt"))
    Console-Write "Alias '$name' is set to $aliasValue" 
  }
}

function DotNetSdk-Alias-Set {
param(
  [string] $name,
  [string] $value
)
  $runtimeFullName = Requested-VersionOrAlias $value
  $aliasFilePath = $userDotNetPath + "\alias\" + $name + ".txt"
  $action = if (Test-Path $aliasFilePath) { "Updating" } else { "Setting" }
  Console-Write "$action alias '$name' to '$runtimeFullName'"
  md ($userDotNetPath + "\alias\") -Force | Out-Null
  $runtimeFullName | Out-File ($aliasFilePath) ascii
}

function DotNetSdk-Unalias {
param(
  [string] $name
)
  $aliasPath=$userDotNetPath + "\alias\" + $name + ".txt"
  if (Test-Path -literalPath "$aliasPath") {
      Console-Write "Removing alias $name"
      Remove-Item -literalPath $aliasPath
  } else {
      Console-Write "Cannot remove alias, '$name' is not a valid alias name"
      $script:exitCode = 1 # Return non-zero exit code for scripting
  }
}

function Locate-DotNetBinFromFullName() {
param(
  [string] $runtimeFullName
)
  $dotnetHome = $env:DOTNET_HOME
  if (!$dotnetHome) {
    $dotnetHome = "$globalDotNetPath;$userDotNetPath"
  }
  foreach($portion in $dotnetHome.Split(';')) {
    $path = [System.Environment]::ExpandEnvironmentVariables($portion)
    $runtimeBin = "$path\runtimes\$runtimeFullName\bin"
    if (Test-Path "$runtimeBin") {
      return $runtimeBin
    }
  }
  return $null
}

function Package-Version() {
param(
  [string] $runtimeFullName
)
  return $runtimeFullName -replace '[^.]*.(.*)', '$1'
}

function Package-Platform() {
param(
  [string] $runtimeFullName
)
  return $runtimeFullName -replace 'dotnet-([^-]*).*', '$1'
}

function Package-Arch() {
param(
  [string] $runtimeFullName
)
  return $runtimeFullName -replace 'dotnet-[^-]*-[^-]*-([^.]*).*', '$1'
}


function Requested-VersionOrAlias() {
param(
  [string] $versionOrAlias
)
  Validate-Full-Package-Name-Arguments-Combination $versionOrAlias

  $runtimeBin = Locate-DotNetBinFromFullName $versionOrAlias

  # If the name specified is an existing package, just use it as is
  if ($runtimeBin -ne $null) {
    return $versionOrAlias
  }

  If (Test-Path ($userDotNetPath + "\alias\" + $versionOrAlias + ".txt")) {
    $aliasValue = Get-Content ($userDotNetPath + "\alias\" + $versionOrAlias + ".txt")
    # Split dotnet-coreclr-win-x86.1.0.0-beta3-10922 into version and name sections
    $parts = $aliasValue.Split('.', 2)
    $pkgVersion = $parts[1]
    # dotnet-coreclr-win-x86
    $parts = $parts[0].Split('-', 4)
    $pkgPlatform = Requested-Platform $parts[1]
    $pkgArchitecture = Requested-Architecture $parts[3]
  } else {
    $pkgVersion = $versionOrAlias
    $pkgPlatform = Requested-Platform $defaultRuntime
    $pkgArchitecture = Requested-Architecture $defaultArch
  }
  return $RuntimePackageName + "-" + $pkgPlatform + "-win-" + $pkgArchitecture + "." + $pkgVersion
}

function Requested-Platform() {
param(
  [string] $default
)
  if (!(String-IsEmptyOrWhitespace($selectedRuntime))) {return $selectedRuntime}
  return $default
}

function Requested-Architecture() {
param(
  [string] $default
)
  if (!(String-IsEmptyOrWhitespace($selectedArch))) {return $selectedArch}
  return $default
}

function Change-Path() {
param(
  [string] $existingPaths,
  [string] $prependPath,
  [string[]] $removePaths
)
  $newPath = $prependPath
  foreach($portion in $existingPaths.Split(';')) {
    $skip = $portion -eq ""
    foreach($removePath in $removePaths) {
      if ($removePath -and ($portion.StartsWith($removePath))) {
        $skip = $true
      }
    }
    if (!$skip) {
      $newPath = $newPath + ";" + $portion
    }
  }
  return $newPath
}

function Set-Path() {
param(
  [string] $newPath
)
  md $userDotNetPath -Force | Out-Null
  $env:Path = $newPath
@"
SET "PATH=$newPath"
"@ | Out-File ($userDotNetPath + "\temp-set-envvars.cmd") ascii
}

function Needs-Elevation() {
  if($AssumeElevated) {
    return $false
  }

  $user = [Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()
  $elevated = $user.IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")
  return -NOT $elevated
}

function Requested-Switches() {
  $arguments = ""
  if ($X86) {$arguments = "$arguments -x86"}
  if ($X64) {$arguments = "$arguments -x64"}
  if ($selectedRuntime) {$arguments = "$arguments -runtime $selectedRuntime"}
  if ($Persistent) {$arguments = "$arguments -persistent"}
  if ($Force) {$arguments = "$arguments -force"}
  if (!$(String-IsEmptyOrWhitespace($Alias))) {$arguments = "$arguments -alias '$Alias'"}
  return $arguments
}

function Validate-And-Santitize-Switches()
{
  if ($X86 -and $X64) {throw "You cannot select both x86 and x64 architectures"}
  
  if ($Runtime) {
    $validRuntimes = "CoreCLR", "CLR"
    $match = $validRuntimes | ? { $_ -like $Runtime } | Select -First 1
    if (!$match) {throw "'$runtime' is not a valid runtime"}
    Set-Variable -Name "selectedRuntime" -Value $match.ToLowerInvariant() -Scope Script
  }

  if($Architecture) {
    $validArchitectures = "x64", "x86"
    $match = $validArchitectures | ? { $_ -like $Architecture } | Select -First 1
    if(!$match) {throw "'$architecture' is not a valid architecture"}
    Set-Variable -Name "selectedArch" -Value $match.ToLowerInvariant() -Scope Script
  }
  else {
    if ($X64) {
      Set-Variable -Name "selectedArch" -Value "x64" -Scope Script
    } elseif ($X86) {
      Set-Variable -Name "selectedArch" -Value "x86" -Scope Script
    }
  }

}

$script:capturedOut = @()
function Console-Write() {
param(
  [Parameter(ValueFromPipeline=$true)]
  [string] $message
)
  if($OutputVariable) {
    # Update the capture output
    $script:capturedOut += @($message)
  }

  if(!$Quiet) {
    if ($useHostOutputMethods) {
      try {
        Write-Host $message
      }
      catch {
        $script:useHostOutputMethods = $false
        Console-Write $message
      }
    }
    else {
      [Console]::WriteLine($message)
    }  
  }
}

function Console-Write-Error() {
param(
  [Parameter(ValueFromPipeline=$true)]
  [string] $message
)
  if ($useHostOutputMethods) {
    try {
      Write-Error $message
    }
    catch {
      $script:useHostOutputMethods = $false
      Console-Write-Error $message
    }
  }
  else {
   [Console]::Error.WriteLine($message)
  }  
}

function Validate-Full-Package-Name-Arguments-Combination() {
param(
	[string] $versionOrAlias
)
	if ($versionOrAlias -like "dotnet-*" -and
	    ($selectedArch -or $selectedRuntime)) {
		throw "Runtime or architecture cannot be specified when using the full package name."
  }
}

$script:exitCode = 0
try {
  Validate-And-Santitize-Switches
  switch -wildcard ($Command + " " + $Args.Count) {
    "setup 0"           {DotNetSdk-Global-Setup}
    "upgrade 0"         {DotNetSdk-Upgrade $false}
    "install 1"         {DotNetSdk-Install $Args[0] $false}
    "list 0"            {DotNetSdk-List}
    "use 1"             {DotNetSdk-Use $Args[0]}
    "alias 0"           {DotNetSdk-Alias-List}
    "alias 1"           {DotNetSdk-Alias-Get $Args[0]}
    "alias 2"           {DotNetSdk-Alias-Set $Args[0] $Args[1]}
    "unalias 1"         {DotNetSdk-Unalias $Args[0]}
    "help 0"            {DotNetSdk-Help}
    " 0"                {DotNetSdk-Help}
    default             {throw "Unknown command"};
  }
}
catch {
  Console-Write-Error $_
  Console-Write "Type 'dotnetsdk help' for help on how to use dotnetsdk."
  $script:exitCode = -1
}
if ($Wait) {
  Console-Write "Press any key to continue ..."
  $x = $host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown,AllowCtrlC")
}

# If the user specified an output variable, push the value up to the parent scope
if($OutputVariable) {
  Set-Variable $OutputVariable $script:capturedOut -Scope 1
}

exit $script:exitCode
