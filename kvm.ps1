param(
  [parameter(Position=0)]
  [string] $Command,
  [string] $Proxy,
  [switch] $Verbosity = $false,
  [alias("g")][switch] $Global = $false,
  [alias("p")][switch] $Persistent = $false,
  [alias("f")][switch] $Force = $false,
  [alias("r")][string] $Runtime,
  [alias("arch")][string] $Architecture,
  [switch] $X86 = $false,
  [switch] $Amd64 = $false,
  #deprecated
  [switch] $X64 = $false,
  #deprecated
  [switch] $Svr50 = $false,
  #deprecated
  [switch] $Svrc50 = $false,
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

$selectedArch=$null;
$defaultArch="x86"
$selectedRuntime=$null
$defaultRuntime="CLR"

# Get or calculate userKrePath
$userKrePath = $env:USER_KRE_PATH
if(!$userKrePath) { $userKrePath = $env:USERPROFILE + "\.kre" }
$userKrePackages = $userKrePath + "\packages"

# Get or calculate globalKrePath
$globalKrePath = $env:GLOBAL_KRE_PATH
if(!$globalKrePath) { $globalKrePath = $env:ProgramFiles + "\KRE" }
$globalKrePackages = $globalKrePath + "\packages"
$feed = $env:KRE_FEED

# In some environments, like Azure Websites, the Write-* cmdlets don't work
$useHostOutputMethods = $true

function String-IsEmptyOrWhitespace([string]$str) {
     return [string]::IsNullOrEmpty($str) -or $str.Trim().length -eq 0
}

if (!$feed)
{
    $feed = "https://www.myget.org/F/aspnetvnext/api/v2";
}

$feed = $feed.TrimEnd("/")

$scriptPath = $myInvocation.MyCommand.Definition

function Kvm-Help {
@"
K Runtime Environment Version Manager - Build 10104

USAGE: kvm <command> [options]

kvm upgrade [-X86][-Amd64] [-r|-Runtime CLR|CoreCLR] [-g|-Global] [-f|-Force] [-Proxy <ADDRESS>] [-NoNative]
  install latest KRE from feed
  set 'default' alias to installed version
  add KRE bin to user PATH environment variable
  -g|-Global        install to machine-wide location
  -f|-Force         upgrade even if latest is already installed
  -Proxy <ADDRESS>  use given address as proxy when accessing remote server (e.g. http://username:password@proxyserver:8080/). Alternatively set proxy using http_proxy environment variable.
  -NoNative         Do not generate native images (Effective only for CoreCLR flavors)

kvm install <semver>|<alias>|<nupkg>|latest [-X86][-Amd64] [-r|-Runtime CLR|CoreCLR] [-a|-Alias <alias>] [-g|-Global] [-f|-Force] [-Proxy <ADDRESS>] [-NoNative]
  <semver>|<alias>  install requested KRE from feed
  <nupkg>           install requested KRE from package on local filesystem
  latest            install latest KRE from feed
  add KRE bin to path of current command line
  -p|-Persistent    add KRE bin to PATH environment variables persistently
  -a|-Alias <alias> set alias <alias> for requested KRE on install
  -g|-Global        install to machine-wide location
  -f|-Force         install even if specified version is already installed
  -Proxy <ADDRESS>  use given address as proxy when accessing remote server (e.g. http://username:password@proxyserver:8080/). Alternatively set proxy using http_proxy environment variable.
  -NoNative         Do not generate native images (Effective only for CoreCLR flavors)

kvm use <semver>|<alias>|<package>|none [-X86][-Amd64] [-r|-Runtime CLR|CoreCLR] [-p|-Persistent] [-g|-Global]
  <semver>|<alias>|<package>  add KRE bin to path of current command line
  none                        remove KRE bin from path of current command line
  -p|-Persistent              add KRE bin to PATH environment variables persistently
  -g|-Global                  combined with -p to change machine PATH instead of user PATH

kvm list
  list KRE versions installed

kvm alias
  list KRE aliases which have been defined

kvm alias <alias>
  display value of the specified alias

kvm alias <alias> <semver>|<alias>|<package> [-X86][-Amd64] [-r|-Runtime CLR|CoreCLR]
  <alias>                      the name of the alias to set
  <semver>|<alias>|<package>   the KRE version to set the alias to. Alternatively use the version of the specified alias

kvm unalias <alias>
  remove the specified alias

"@ -replace "`n","`r`n" | Console-Write
}

function Kvm-Global-Setup {
  $kvmBinPath = "$userKrePath\bin"

  If (Needs-Elevation)
  {
    $arguments = "-ExecutionPolicy unrestricted & '$scriptPath' setup -global -wait"
    Start-Process "$psHome\powershell.exe" -Verb runAs -ArgumentList $arguments -Wait
    Console-Write "Adding $kvmBinPath to process PATH"
    Set-Path (Change-Path $env:Path $kvmBinPath ($kvmBinPath))
    Console-Write "Adding $globalKrePath;%USERPROFILE%\.kre to process KRE_HOME"
    $envKreHome = $env:KRE_HOME
    $envKreHome = Change-Path $envKreHome "%USERPROFILE%\.kre" ("%USERPROFILE%\.kre")
    $envKreHome = Change-Path $envKreHome $globalKrePath ($globalKrePath)
    $env:KRE_HOME = $envKreHome
    Console-Write "Setup complete"
    break
  }

  $scriptFolder = [System.IO.Path]::GetDirectoryName($scriptPath)

  Console-Write "Copying file $kvmBinPath\kvm.ps1"
  md $kvmBinPath -Force | Out-Null
  copy "$scriptFolder\kvm.ps1" "$kvmBinPath\kvm.ps1"

  Console-Write "Copying file $kvmBinPath\kvm.cmd"
  copy "$scriptFolder\kvm.cmd" "$kvmBinPath\kvm.cmd"

  Console-Write "Adding $kvmBinPath to process PATH"
  Set-Path (Change-Path $env:Path $kvmBinPath ($kvmBinPath))

  Console-Write "Adding $kvmBinPath to user PATH"
  $userPath = [Environment]::GetEnvironmentVariable("Path", [System.EnvironmentVariableTarget]::User)
  $userPath = Change-Path $userPath $kvmBinPath ($kvmBinPath)
  [Environment]::SetEnvironmentVariable("Path", $userPath, [System.EnvironmentVariableTarget]::User)

  Console-Write "Adding $globalKrePath;%USERPROFILE%\.kre to process KRE_HOME"
  $envKreHome = $env:KRE_HOME
  $envKreHome = Change-Path $envKreHome "%USERPROFILE%\.kre" ("%USERPROFILE%\.kre")
  $envKreHome = Change-Path $envKreHome $globalKrePath ($globalKrePath)
  $env:KRE_HOME = $envKreHome

  Console-Write "Adding $globalKrePath;%USERPROFILE%\.kre to machine KRE_HOME"
  $machineKreHome = [Environment]::GetEnvironmentVariable("KRE_HOME", [System.EnvironmentVariableTarget]::Machine)
  $machineKreHome = Change-Path $machineKreHome "%USERPROFILE%\.kre" ("%USERPROFILE%\.kre")
  $machineKreHome = Change-Path $machineKreHome $globalKrePath ($globalKrePath)
  [Environment]::SetEnvironmentVariable("KRE_HOME", $machineKreHome, [System.EnvironmentVariableTarget]::Machine)
}

function Kvm-Upgrade {
param(
  [boolean] $isGlobal
)
  $Persistent = $true
  $Alias="default"
  Kvm-Install "latest" $isGlobal
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

function Kvm-Find-Latest {
param(
  [string] $platform,
  [string] $architecture
)
  Console-Write "Determining latest version"

  $url = "$feed/GetUpdates()?packageIds=%27KRE-$platform-$architecture%27&versions=%270.0%27&includePrerelease=true&includeAllVersions=false"

  $wc = New-Object System.Net.WebClient
  Add-Proxy-If-Specified($wc)
  [xml]$xml = $wc.DownloadString($url)

  $version = Select-Xml "//d:Version" -Namespace @{d='http://schemas.microsoft.com/ado/2007/08/dataservices'} $xml

  if (String-IsEmptyOrWhitespace($version)) {
    throw "There are no packages for platform '$platform', architecture '$architecture' in the feed '$feed'"
  }

  return $version
}

function Do-Kvm-Download {
param(
  [string] $kreFullName,
  [string] $packagesFolder
)
  $parts = $kreFullName.Split(".", 2)

  $url = "$feed/package/" + $parts[0] + "/" + $parts[1]
  $kreFolder = Join-Path $packagesFolder $kreFullName
  $kreFile = Join-Path $kreFolder "$kreFullName.nupkg"

  If (Test-Path $kreFolder) {
    if($Force)
    {
      rm $kreFolder -Recurse -Force
    } else {
      Console-Write "$kreFullName already installed."
      return;
    }
  }

  Console-Write "Downloading $kreFullName from $feed"

  #Downloading to temp location
  $kreTempDownload = Join-Path $packagesFolder "temp"
  $tempKreFile = Join-Path $kreTempDownload "$kreFullName.nupkg"

  if(Test-Path $kreTempDownload) {
    del "$kreTempDownload\*" -recurse
  } else {
    md $kreTempDownload -Force | Out-Null
  }

  $wc = New-Object System.Net.WebClient
  Add-Proxy-If-Specified($wc)
  $wc.DownloadFile($url, $tempKreFile)

  Do-Kvm-Unpack $tempKreFile $kreTempDownload

  md $kreFolder -Force | Out-Null
  Console-Write "Installing to $kreFolder"
  mv "$kreTempDownload\*" $kreFolder
  Remove-Item "$kreTempDownload" -Force | Out-Null
}

function Do-Kvm-Unpack {
param(
  [string] $kreFile,
  [string] $kreFolder
)
  Console-Write "Unpacking to $kreFolder"

  $compressionLib = [System.Reflection.Assembly]::LoadWithPartialName('System.IO.Compression.FileSystem')
    
  if($compressionLib -eq $null) {
      try {
          # Shell will not recognize nupkg as a zip and throw, so rename it to zip
          $kreZip = [System.IO.Path]::ChangeExtension($kreFile, "zip")
          Rename-Item $kreFile $kreZip
          # Use the shell to uncompress the nupkg
          $shell_app=new-object -com shell.application
          $zip_file = $shell_app.namespace($kreZip)
          $destination = $shell_app.namespace($kreFolder)
          $destination.Copyhere($zip_file.items(), 0x14) #0x4 = don't show UI, 0x10 = overwrite files
      }
      finally {
        # make it a nupkg again
        Rename-Item $kreZip $kreFile
      }
  } else {
      [System.IO.Compression.ZipFile]::ExtractToDirectory($kreFile, $kreFolder)
  }

  If (Test-Path ($kreFolder + "\[Content_Types].xml")) {
    Remove-Item ($kreFolder + "\[Content_Types].xml")
  }
  If (Test-Path ($kreFolder + "\_rels\")) {
    Remove-Item ($kreFolder + "\_rels\") -Force -Recurse
  }
  If (Test-Path ($kreFolder + "\package\")) {
    Remove-Item ($kreFolder + "\package\") -Force -Recurse
  }
}

function Kvm-Install {
param(
  [string] $versionOrAlias,
  [boolean] $isGlobal
)
  if ($versionOrAlias -eq "latest") {
    $versionOrAlias = Kvm-Find-Latest (Requested-Platform $defaultRuntime) (Requested-Architecture $defaultArch)
  }

  if ($versionOrAlias.EndsWith(".nupkg")) {
    $kreFullName = [System.IO.Path]::GetFileNameWithoutExtension($versionOrAlias)
  } else {
    $kreFullName =  Requested-VersionOrAlias $versionOrAlias
  }

  if ($isGlobal) {
    if (Needs-Elevation) {
      $arguments = "-ExecutionPolicy unrestricted & '$scriptPath' install '$versionOrAlias' -global $(Requested-Switches) -wait"
      Start-Process "$psHome\powershell.exe" -Verb runAs -ArgumentList $arguments -Wait
      Kvm-Use $kreFullName
      break
    }
    $packageFolder = $globalKrePackages
  } else {
    $packageFolder = $userKrePackages
  }

  if ($versionOrAlias.EndsWith(".nupkg")) {
    Set-Variable -Name "selectedArch" -Value (Package-Arch $kreFullName) -Scope Script
    Set-Variable -Name "selectedRuntime" -Value (Package-Platform $kreFullName) -Scope Script

    $kreFolder = "$packageFolder\$kreFullName"
    $folderExists = Test-Path $kreFolder

    if ($folderExists -and $Force) {
      del $kreFolder -Recurse -Force
      $folderExists = $false;
    }

    if ($folderExists) {
      Console-Write "Target folder '$kreFolder' already exists"
    } else {
      $tempUnpackFolder = Join-Path $packageFolder "temp"
      $tempKreFile = Join-Path $tempUnpackFolder "$kreFullName.nupkg"

      if(Test-Path $tempUnpackFolder) {
          del "$tempUnpackFolder\*" -recurse
      } else {
          md $tempUnpackFolder -Force | Out-Null
      }
      copy $versionOrAlias $tempKreFile

      Do-Kvm-Unpack $tempKreFile $tempUnpackFolder
      md $kreFolder -Force | Out-Null
      Console-Write "Installing to $kreFolder"
      mv "$tempUnpackFolder\*" $kreFolder
      Remove-Item "$tempUnpackFolder" -Force | Out-Null
    }

    $packageVersion = Package-Version $kreFullName

    Kvm-Use $packageVersion
    if (!$(String-IsEmptyOrWhitespace($Alias))) {
        Kvm-Alias-Set $Alias $packageVersion
    }
  }
  else
  {
    Do-Kvm-Download $kreFullName $packageFolder
    Kvm-Use $versionOrAlias
    if (!$(String-IsEmptyOrWhitespace($Alias))) {
        Kvm-Alias-Set "$Alias" $versionOrAlias
    }
  }

  if ($kreFullName.Contains("CoreCLR")) {
    if ($NoNative) {
      Console-Write "Native image generation is skipped"
    }
    else {
      Console-Write "Compiling native images for $kreFullName to improve startup performance..."
      Start-Process "k-crossgen" -Wait
      Console-Write "Finished native image compilation."
    }
  }
}

function Kvm-List {
  $kreHome = $env:KRE_HOME
  if (!$kreHome) {
    $kreHome = "$globalKrePath;$userKrePath"
  }

  md ($userKrePath + "\alias\") -Force | Out-Null
  $aliases = Get-ChildItem ($userKrePath + "\alias\") | Select @{label='Alias';expression={$_.BaseName}}, @{label='Name';expression={Get-Content $_.FullName }}

  $items = @()
  foreach($portion in $kreHome.Split(';')) {
    $path = [System.Environment]::ExpandEnvironmentVariables($portion)
    if (Test-Path("$path\packages")) {
      $items += Get-ChildItem ("$path\packages\KRE-*") | List-Parts $aliases
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
  $parts2 = $parts1[0].Split('-', 3)
  return New-Object PSObject -Property @{
    Active = if ($active) { "*" } else { "" }
    Version = $parts1[1]
    Runtime = $parts2[1]
    Architecture = $parts2[2]
    Location = $_.Parent.FullName
    Alias = $fullAlias
  }
}

function Kvm-Global-Use {
param(
  [string] $versionOrAlias
)
  Validate-Full-Package-Name-Arguments-Combination $versionOrAlias

  If (Needs-Elevation) {
    $arguments = "-ExecutionPolicy unrestricted & '$scriptPath' use '$versionOrAlias' -global $(Requested-Switches) -wait"
    Start-Process "$psHome\powershell.exe" -Verb runAs -ArgumentList $arguments -Wait
    Kvm-Use $versionOrAlias
    break
  }

  Kvm-Use "$versionOrAlias"

  if ($versionOrAlias -eq "none") {
    if ($Persistent) {
      Console-Write "Removing KRE from machine PATH"
      $machinePath = [Environment]::GetEnvironmentVariable("Path", [System.EnvironmentVariableTarget]::Machine)
      $machinePath = Change-Path $machinePath "" ($globalKrePackages, $userKrePackages)
      [Environment]::SetEnvironmentVariable("Path", $machinePath, [System.EnvironmentVariableTarget]::Machine)
    }
    return;
  }

  $kreFullName = Requested-VersionOrAlias "$versionOrAlias"
  $kreBin = Locate-KreBinFromFullName $kreFullName
  if ($kreBin -eq $null) {
    throw "Cannot find $kreFullName, do you need to run 'kvm install $versionOrAlias'?"
  }

  if ($Persistent) {
    Console-Write "Adding $kreBin to machine PATH"
    $machinePath = [Environment]::GetEnvironmentVariable("Path", [System.EnvironmentVariableTarget]::Machine)
    $machinePath = Change-Path $machinePath $kreBin ($globalKrePackages, $userKrePackages)
    [Environment]::SetEnvironmentVariable("Path", $machinePath, [System.EnvironmentVariableTarget]::Machine)
  }
}

function Kvm-Use {
param(
  [string] $versionOrAlias
)
  Validate-Full-Package-Name-Arguments-Combination $versionOrAlias

  if ($versionOrAlias -eq "none") {
    Console-Write "Removing KRE from process PATH"
    Set-Path (Change-Path $env:Path "" ($globalKrePackages, $userKrePackages))

    if ($Persistent) {
      Console-Write "Removing KRE from user PATH"
      $userPath = [Environment]::GetEnvironmentVariable("Path", [System.EnvironmentVariableTarget]::User)
      $userPath = Change-Path $userPath "" ($globalKrePackages, $userKrePackages)
      [Environment]::SetEnvironmentVariable("Path", $userPath, [System.EnvironmentVariableTarget]::User)
    }
    return;
  }

  $kreFullName = Requested-VersionOrAlias $versionOrAlias

  $kreBin = Locate-KreBinFromFullName $kreFullName
  if ($kreBin -eq $null) {
    throw "Cannot find $kreFullName, do you need to run 'kvm install $versionOrAlias'?"
  }

  Console-Write "Adding $kreBin to process PATH"
  Set-Path (Change-Path $env:Path $kreBin ($globalKrePackages, $userKrePackages))

  if ($Persistent) {
    Console-Write "Adding $kreBin to user PATH"
    $userPath = [Environment]::GetEnvironmentVariable("Path", [System.EnvironmentVariableTarget]::User)
    $userPath = Change-Path $userPath $kreBin ($globalKrePackages, $userKrePackages)
    [Environment]::SetEnvironmentVariable("Path", $userPath, [System.EnvironmentVariableTarget]::User)
  }
}

function Kvm-Alias-List {
  md ($userKrePath + "\alias\") -Force | Out-Null

  Get-ChildItem ($userKrePath + "\alias\") | Select @{label='Alias';expression={$_.BaseName}}, @{label='Name';expression={Get-Content $_.FullName }} | Format-Table -AutoSize
}

function Kvm-Alias-Get {
param(
  [string] $name
)
  md ($userKrePath + "\alias\") -Force | Out-Null
  $aliasFilePath=$userKrePath + "\alias\" + $name + ".txt"
  if (!(Test-Path $aliasFilePath)) {
    Console-Write "Alias '$name' does not exist"
    $script:exitCode = 1 # Return non-zero exit code for scripting
  } else {
    $aliasValue = (Get-Content ($userKrePath + "\alias\" + $name + ".txt"))
    Console-Write "Alias '$name' is set to $aliasValue" 
  }
}

function Kvm-Alias-Set {
param(
  [string] $name,
  [string] $value
)
  $kreFullName = Requested-VersionOrAlias $value
  $aliasFilePath = $userKrePath + "\alias\" + $name + ".txt"
  $action = if (Test-Path $aliasFilePath) { "Updating" } else { "Setting" }
  Console-Write "$action alias '$name' to '$kreFullName'"
  md ($userKrePath + "\alias\") -Force | Out-Null
  $kreFullName | Out-File ($aliasFilePath) ascii
}

function Kvm-Unalias {
param(
  [string] $name
)
  $aliasPath=$userKrePath + "\alias\" + $name + ".txt"
  if (Test-Path -literalPath "$aliasPath") {
      Console-Write "Removing alias $name"
      Remove-Item -literalPath $aliasPath
  } else {
      Console-Write "Cannot remove alias, '$name' is not a valid alias name"
      $script:exitCode = 1 # Return non-zero exit code for scripting
  }
}

function Locate-KreBinFromFullName() {
param(
  [string] $kreFullName
)
  $kreHome = $env:KRE_HOME
  if (!$kreHome) {
    $kreHome = "$globalKrePath;$userKrePath"
  }
  foreach($portion in $kreHome.Split(';')) {
    $path = [System.Environment]::ExpandEnvironmentVariables($portion)
    $kreBin = "$path\packages\$kreFullName\bin"
    if (Test-Path "$kreBin") {
      return $kreBin
    }
  }
  return $null
}

function Package-Version() {
param(
  [string] $kreFullName
)
  return $kreFullName -replace '[^.]*.(.*)', '$1'
}

function Package-Platform() {
param(
  [string] $kreFullName
)
  return $kreFullName -replace 'KRE-([^-]*).*', '$1'
}

function Package-Arch() {
param(
  [string] $kreFullName
)
  return $kreFullName -replace 'KRE-[^-]*-([^.]*).*', '$1'
}


function Requested-VersionOrAlias() {
param(
  [string] $versionOrAlias
)
  Validate-Full-Package-Name-Arguments-Combination $versionOrAlias

  $kreBin = Locate-KreBinFromFullName $versionOrAlias

  # If the name specified is an existing package, just use it as is
  if ($kreBin -ne $null) {
    return $versionOrAlias
  }

  If (Test-Path ($userKrePath + "\alias\" + $versionOrAlias + ".txt")) {
    $aliasValue = Get-Content ($userKrePath + "\alias\" + $versionOrAlias + ".txt")
    $parts = $aliasValue.Split('.', 2)
    $pkgVersion = $parts[1]
    $parts =$parts[0].Split('-', 3)
    $pkgPlatform = Requested-Platform $parts[1]
    $pkgArchitecture = Requested-Architecture $parts[2]
  } else {
    $pkgVersion = $versionOrAlias
    $pkgPlatform = Requested-Platform $defaultRuntime
    $pkgArchitecture = Requested-Architecture $defaultArch
  }
  return "KRE-" + $pkgPlatform + "-" + $pkgArchitecture + "." + $pkgVersion
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
      if ($portion.StartsWith($removePath)) {
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
  md $userKrePath -Force | Out-Null
  $env:Path = $newPath
@"
SET "PATH=$newPath"
"@ | Out-File ($userKrePath + "\run-once.cmd") ascii
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
  if ($Amd64) {$arguments = "$arguments -amd64"}
  #deprecated
  if ($X64) {$arguments = "$arguments -x64"}
  if ($selectedRuntime) {$arguments = "$arguments -runtime $selectedRuntime"}
  if ($Persistent) {$arguments = "$arguments -persistent"}
  if ($Force) {$arguments = "$arguments -force"}
  if (!$(String-IsEmptyOrWhitespace($Alias))) {$arguments = "$arguments -alias '$Alias'"}
  return $arguments
}

function Validate-And-Santitize-Switches()
{
  if ($Svr50 -and $Runtime) {throw "You cannot select both the -runtime switch and the -svr50 runtimes"}
  if ($Svrc50 -and $Runtime) {throw "You cannot select both the -runtime switch and the -svrc50 runtimes"}
  if ($X86 -and $Amd64) {throw "You cannot select both x86 and amd64 architectures"}
  if ($X86 -and $X64) {throw "You cannot select both x86 and x64 architectures"}
  if ($X64 -and $Amd64) {throw "You cannot select both x64 and amd64 architectures"}

  if ($Runtime) {
    $validRuntimes = "CoreCLR", "CLR", "svr50", "svrc50"
    $match = $validRuntimes | ? { $_ -like $Runtime } | Select -First 1
    if (!$match) {throw "'$runtime' is not a valid runtime"}
    Set-Variable -Name "selectedRuntime" -Value $match -Scope Script
  } elseif ($Svr50) {
    Console-Write "Warning: -svr50 is deprecated, use -runtime CLR for new packages."
    Set-Variable -Name "selectedRuntime" -Value "svr50" -Scope Script
  } elseif ($Svrc50) {
    Console-Write "Warning: -svrc50 is deprecated, use -runtime CoreCLR for new packages."
    Set-Variable -Name "selectedRuntime" -Value "svrc50" -Scope Script
  }

  if($Architecture) {
    $validArchitectures = "amd64", "x86"
    $match = $validArchitectures | ? { $_ -like $Architecture } | Select -First 1
    if(!$match) {throw "'$architecture' is not a valid architecture"}
    Set-Variable -Name "selectedArch" -Value $match -Scope Script
  }
  else {
    if ($X64) {
      Console-Write "Warning: -x64 is deprecated, use -amd64 for new packages."
      Set-Variable -Name "selectedArch" -Value "x64" -Scope Script
    } elseif ($Amd64) {
      Set-Variable -Name "selectedArch" -Value "amd64" -Scope Script
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
	if ($versionOrAlias -like "KRE-*" -and
	    ($selectedArch -or $selectedRuntime)) {
		throw "Runtime or architecture cannot be specified when using the full package name."
  }
}

$script:exitCode = 0
try {
  Validate-And-Santitize-Switches
  if ($Global) {
    switch -wildcard ($Command + " " + $Args.Count) {
      "setup 0"           {Kvm-Global-Setup}
      "upgrade 0"         {Kvm-Upgrade $true}
      "install 1"         {Kvm-Install $Args[0] $true}
      "use 1"             {Kvm-Global-Use $Args[0]}
      default             {throw "Unknown command, or global switch not supported"};
    }
  } else {
    switch -wildcard ($Command + " " + $Args.Count) {
      "setup 0"           {Kvm-Global-Setup}
      "upgrade 0"         {Kvm-Upgrade $false}
      "install 1"         {Kvm-Install $Args[0] $false}
      "list 0"            {Kvm-List}
      "use 1"             {Kvm-Use $Args[0]}
      "alias 0"           {Kvm-Alias-List}
      "alias 1"           {Kvm-Alias-Get $Args[0]}
      "alias 2"           {Kvm-Alias-Set $Args[0] $Args[1]}
      "unalias 1"         {Kvm-Unalias $Args[0]}
      "help 0"            {Kvm-Help}
      " 0"                {Kvm-Help}
      default             {throw "Unknown command"};
    }
  }
}
catch {
  Console-Write-Error $_
  Console-Write "Type 'kvm help' for help on how to use kvm."
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
