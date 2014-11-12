param(
  [parameter(Position=0)]
  [string] $command,
  [string] $proxy,
  [switch] $verbosity = $false,
  [alias("g")][switch] $global = $false,
  [alias("p")][switch] $persistent = $false,
  [alias("f")][switch] $force = $false,
  [alias("r")][string] $runtime,
  [switch] $x86 = $false,
  [switch] $amd64 = $false,
  #deprecated
  [switch] $x64 = $false,
  #deprecated
  [switch] $svr50 = $false,
  #deprecated
  [switch] $svrc50 = $false,
  [alias("w")][switch] $wait = $false,
  [alias("a")]
  [string] $alias = $null,
  [parameter(Position=1, ValueFromRemainingArguments=$true)]
  [string[]]$args=@()
)

$selectedArch=$null;
$defaultArch="x86"
$selectedRuntime=$null
$defaultRuntime="CLR"
$userKrePath = $env:USERPROFILE + "\.kre"
$userKrePackages = $userKrePath + "\packages"
$globalKrePath = $env:ProgramFiles + "\KRE"
$globalKrePackages = $globalKrePath + "\packages"
$feed = $env:KRE_NUGET_API_URL

function String-IsEmptyOrWhitespace([string]$str) {
     return [string]::IsNullOrEmpty($str) -or $str.Trim().length -eq 0
}

if (!$feed)
{
 $feed = "https://www.nuget.org/api/v2";
}

$scriptPath = $myInvocation.MyCommand.Definition

function Kvm-Help {
@"
K Runtime Environment Version Manager - Build 10017

USAGE: kvm <command> [options]

kvm upgrade [-x86][-amd64] [-r|-runtime CLR|CoreCLR] [-g|-global] [-f|-force] [-proxy <ADDRESS>]
  install latest KRE from feed
  set 'default' alias to installed version
  add KRE bin to user PATH environment variable
  -g|-global        install to machine-wide location
  -f|-force         upgrade even if latest is already installed
  -proxy <ADDRESS>  use given address as proxy when accessing remote server

kvm install <semver>|<alias>|<nupkg>|latest [-x86][-amd64] [-r|-runtime CLR|CoreCLR] [-a|-alias <alias>] [-g|-global] [-f|-force]
  <semver>|<alias>  install requested KRE from feed
  <nupkg>           install requested KRE from package on local filesystem
  latest            install latest KRE from feed
  add KRE bin to path of current command line
  -p|-persistent    add KRE bin to PATH environment variables persistently
  -a|-alias <alias> set alias <alias> for requested KRE on install
  -g|-global        install to machine-wide location
  -f|-force         install even if specified version is already installed

kvm use <semver>|<alias>|none [-x86][-amd64] [-r|-runtime CLR|CoreCLR] [-p|-persistent] [-g|-global]
  <semver>|<alias>  add KRE bin to path of current command line
  none              remove KRE bin from path of current command line
  -p|-persistent    add KRE bin to PATH environment variables persistently
  -g|-global        combined with -p to change machine PATH instead of user PATH

kvm list
  list KRE versions installed

kvm alias
  list KRE aliases which have been defined

kvm alias <alias>
  display value of the specified alias

kvm alias <alias> <semver>|<alias> [-x86][-amd64] [-r|-runtime CLR|CoreCLR]
  <alias>            The name of the alias to set
  <semver>|<alias>   The KRE version to set the alias to. Alternatively use the version of the specified alias

kvm unalias <alias>
  remove the specified alias

"@ -replace "`n","`r`n" | Write-Host
}

function Kvm-Global-Setup {
  $kvmBinPath = "$userKrePath\bin"

  If (Needs-Elevation)
  {
    $arguments = "-ExecutionPolicy unrestricted & '$scriptPath' setup -global -wait"
    Start-Process "$psHome\powershell.exe" -Verb runAs -ArgumentList $arguments -Wait
    Write-Host "Adding $kvmBinPath to process PATH"
    Set-Path (Change-Path $env:Path $kvmBinPath ($kvmBinPath))
    Write-Host "Adding $globalKrePath;%USERPROFILE%\.kre to process KRE_HOME"
    $envKreHome = $env:KRE_HOME
    $envKreHome = Change-Path $envKreHome "%USERPROFILE%\.kre" ("%USERPROFILE%\.kre")
    $envKreHome = Change-Path $envKreHome $globalKrePath ($globalKrePath)
    $env:KRE_HOME = $envKreHome
    Write-Host "Setup complete"
    break
  }

  $scriptFolder = [System.IO.Path]::GetDirectoryName($scriptPath)

  Write-Host "Copying file $kvmBinPath\kvm.ps1"
  md $kvmBinPath -Force | Out-Null
  copy "$scriptFolder\kvm.ps1" "$kvmBinPath\kvm.ps1"

  Write-Host "Copying file $kvmBinPath\kvm.cmd"
  copy "$scriptFolder\kvm.cmd" "$kvmBinPath\kvm.cmd"

  Write-Host "Adding $kvmBinPath to process PATH"
  Set-Path (Change-Path $env:Path $kvmBinPath ($kvmBinPath))

  Write-Host "Adding $kvmBinPath to user PATH"
  $userPath = [Environment]::GetEnvironmentVariable("Path", [System.EnvironmentVariableTarget]::User)
  $userPath = Change-Path $userPath $kvmBinPath ($kvmBinPath)
  [Environment]::SetEnvironmentVariable("Path", $userPath, [System.EnvironmentVariableTarget]::User)

  Write-Host "Adding $globalKrePath;%USERPROFILE%\.kre to process KRE_HOME"
  $envKreHome = $env:KRE_HOME
  $envKreHome = Change-Path $envKreHome "%USERPROFILE%\.kre" ("%USERPROFILE%\.kre")
  $envKreHome = Change-Path $envKreHome $globalKrePath ($globalKrePath)
  $env:KRE_HOME = $envKreHome

  Write-Host "Adding $globalKrePath;%USERPROFILE%\.kre to machine KRE_HOME"
  $machineKreHome = [Environment]::GetEnvironmentVariable("KRE_HOME", [System.EnvironmentVariableTarget]::Machine)
  $machineKreHome = Change-Path $machineKreHome "%USERPROFILE%\.kre" ("%USERPROFILE%\.kre")
  $machineKreHome = Change-Path $machineKreHome $globalKrePath ($globalKrePath)
  [Environment]::SetEnvironmentVariable("KRE_HOME", $machineKreHome, [System.EnvironmentVariableTarget]::Machine)
}

function Kvm-Global-Upgrade {
  $persistent = $true
  $alias="default"
  $versionOrAlias = Kvm-Find-Latest $selectedRuntime $selectedArch
  If (Needs-Elevation) {
    $arguments = "-ExecutionPolicy unrestricted & '$scriptPath' install '$versionOrAlias' -global $(Requested-Switches) -wait"
    Start-Process "$psHome\powershell.exe" -Verb runAs -ArgumentList $arguments -Wait
    Kvm-Set-Global-Process-Path $versionOrAlias
    break
  }
  Kvm-Install $versionOrAlias $true
}

function Kvm-Upgrade {
  $persistent = $true
  $alias="default"
  Kvm-Install "latest" $false
}

function Add-Proxy-If-Specified {
param(
  [System.Net.WebClient] $wc
)
  if (!$proxy) {
    $proxy = $env:http_proxy
  }
  if ($proxy) {
    $wp = New-Object System.Net.WebProxy($proxy)
    $pb = New-Object UriBuilder($proxy)
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
  Write-Host "Determining latest version"

  $url = "$feed/GetUpdates()?packageIds=%27KRE-$platform-$architecture%27&versions=%270.0%27&includePrerelease=true&includeAllVersions=false"

  $wc = New-Object System.Net.WebClient
  $wc.Credentials = new-object System.Net.NetworkCredential("aspnetreadonly", "4d8a2d9c-7b80-4162-9978-47e918c9658c")
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
    if($force)
    {
      rm $kreFolder -Recurse -Force
    } else {
      Write-Host "$kreFullName already installed."
      return;
    }
  }

  Write-Host "Downloading" $kreFullName "from $feed"

  #Downloading to temp location
  $kreTempDownload = Join-Path $packagesFolder "temp"
  $tempKreFile = Join-Path $kreTempDownload "$kreFullName.nupkg"

  if(Test-Path $kreTempDownload) {
    del "$kreTempDownload\*" -recurse
  } else {
    md $kreTempDownload -Force | Out-Null
  }

  $wc = New-Object System.Net.WebClient
  $wc.Credentials = new-object System.Net.NetworkCredential("aspnetreadonly", "4d8a2d9c-7b80-4162-9978-47e918c9658c")
  Add-Proxy-If-Specified($wc)
  $wc.DownloadFile($url, $tempKreFile)

  Do-Kvm-Unpack $tempKreFile $kreTempDownload

  md $kreFolder -Force | Out-Null
  Write-Host "Installing to $kreFolder"
  mv "$kreTempDownload\*" $kreFolder
  Remove-Item "$kreTempDownload" -Force | Out-Null
}

function Do-Kvm-Unpack {
param(
  [string] $kreFile,
  [string] $kreFolder
)
  Write-Host "Unpacking to" $kreFolder
  try {
    #Shell will not recognize nupkg as a zip and throw, so rename it to zip
    $kreZip = [System.IO.Path]::ChangeExtension($kreFile, "zip")
    Rename-Item $kreFile $kreZip
    #Use the shell to uncompress the nupkg
    $shell_app=new-object -com shell.application
    $zip_file = $shell_app.namespace($kreZip)
    $destination = $shell_app.namespace($kreFolder)
    $destination.Copyhere($zip_file.items(), 0x14) #0x4 = don't show UI, 0x10 = overwrite files
  }
  finally {
    #make it a nupkg again
    Rename-Item $kreZip $kreFile
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
      Kvm-Set-Global-Process-Path $versionOrAlias
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

    if ($folderExists -and $force) {
      del $kreFolder -Recurse -Force
      $folderExists = $false;
    }

    if ($folderExists) {
      Write-Host "Target folder '$kreFolder' already exists"
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
      Write-Host "Installing to $kreFolder"
      mv "$tempUnpackFolder\*" $kreFolder
      Remove-Item "$tempUnpackFolder" -Force | Out-Null
    }

    $packageVersion = Package-Version $kreFullName

    Kvm-Use $packageVersion
    if (!$(String-IsEmptyOrWhitespace($alias))) {
        Kvm-Alias-Set $alias $packageVersion
    }
  }
  else
  {
    Do-Kvm-Download $kreFullName $packageFolder
    Kvm-Use $versionOrAlias
    if (!$(String-IsEmptyOrWhitespace($alias))) {
        Kvm-Alias-Set "$alias" $versionOrAlias
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
    if ($portion.StartsWith($_.FullName)) {
      $active = $true
    }
  }

  $fullAlias=""
  $delim=""

  foreach($alias in $aliases){
    if($_.Name.Split('\', 2).Contains($alias.Name)){
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
  If (Needs-Elevation) {
    $arguments = "-ExecutionPolicy unrestricted & '$scriptPath' use '$versionOrAlias' -global $(Requested-Switches) -wait"
    Start-Process "$psHome\powershell.exe" -Verb runAs -ArgumentList $arguments -Wait
    Kvm-Set-Global-Process-Path $versionOrAlias
    break
  }

  Kvm-Set-Global-Process-Path "$versionOrAlias"

  if ($versionOrAlias -eq "none") {
    if ($persistent) {
      Write-Host "Removing KRE from machine PATH"
      $machinePath = [Environment]::GetEnvironmentVariable("Path", [System.EnvironmentVariableTarget]::Machine)
      $machinePath = Change-Path $machinePath "" ($globalKrePackages, $userKrePackages)
      [Environment]::SetEnvironmentVariable("Path", $machinePath, [System.EnvironmentVariableTarget]::Machine)
    }
    return;
  }

  $kreFullName = Requested-VersionOrAlias "$versionOrAlias"
  $kreBin = Locate-KreBinFromFullName $kreFullName
  if ($kreBin -eq $null) {
    Write-Host "Cannot find $kreFullName, do you need to run 'kvm install $versionOrAlias'?"
    return
  }

  if ($persistent) {
    Write-Host "Adding $kreBin to machine PATH"
    $machinePath = [Environment]::GetEnvironmentVariable("Path", [System.EnvironmentVariableTarget]::Machine)
    $machinePath = Change-Path $machinePath $kreBin ($globalKrePackages, $userKrePackages)
    [Environment]::SetEnvironmentVariable("Path", $machinePath, [System.EnvironmentVariableTarget]::Machine)
  }
}

function Kvm-Set-Global-Process-Path {
param(
  [string] $versionOrAlias
)
  if ($versionOrAlias -eq "none") {
    Write-Host "Removing KRE from process PATH"
    Set-Path (Change-Path $env:Path "" ($globalKrePackages, $userKrePackages))
    return
  }

  $kreFullName = Requested-VersionOrAlias $versionOrAlias
  $kreBin = Locate-KreBinFromFullName $kreFullName
  if ($kreBin -eq $null) {
    Write-Host "Cannot find $kreFullName, do you need to run 'kvm install $versionOrAlias'?"
    return
  }

  Write-Host "Adding" $kreBin "to process PATH"
  Set-Path (Change-Path $env:Path $kreBin ($globalKrePackages, $userKrePackages))
}

function Kvm-Use {
param(
  [string] $versionOrAlias
)
  if ($versionOrAlias -eq "none") {
    Write-Host "Removing KRE from process PATH"
    Set-Path (Change-Path $env:Path "" ($globalKrePackages, $userKrePackages))

    if ($persistent) {
      Write-Host "Removing KRE from user PATH"
      $userPath = [Environment]::GetEnvironmentVariable("Path", [System.EnvironmentVariableTarget]::User)
      $userPath = Change-Path $userPath "" ($globalKrePackages, $userKrePackages)
      [Environment]::SetEnvironmentVariable("Path", $userPath, [System.EnvironmentVariableTarget]::User)
    }
    return;
  }

  $kreFullName = Requested-VersionOrAlias $versionOrAlias

  $kreBin = Locate-KreBinFromFullName $kreFullName
  if ($kreBin -eq $null) {
    Write-Host "Cannot find $kreFullName, do you need to run 'kvm install $versionOrAlias'?"
    return
  }

  Write-Host "Adding" $kreBin "to process PATH"
  Set-Path (Change-Path $env:Path $kreBin ($globalKrePackages, $userKrePackages))

  if ($persistent) {
    Write-Host "Adding $kreBin to user PATH"
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
    Write-Host "Alias '$name' does not exist"
  } else {
    Write-Host "Alias '$name' is set to" (Get-Content ($userKrePath + "\alias\" + $name + ".txt"))
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
  Write-Host "$action alias '$name' to '$kreFullName'"
  md ($userKrePath + "\alias\") -Force | Out-Null
  $kreFullName | Out-File ($aliasFilePath) ascii
}

function Kvm-Unalias {
param(
  [string] $name
)
  $aliasPath=$userKrePath + "\alias\" + $name + ".txt"
  if (Test-Path -literalPath "$aliasPath") {
      Write-Host "Removing alias $name"
      Remove-Item -literalPath $aliasPath
  } else {
      Write-Host "Cannot remove alias, '$name' is not a valid alias name"
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
  $user = [Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()
  $elevated = $user.IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")
  return -NOT $elevated
}

function Requested-Switches() {
  $arguments = ""
  if ($x86) {$arguments = "$arguments -x86"}
  if ($amd64) {$arguments = "$arguments -amd64"}
  #deprecated
  if ($x64) {$arguments = "$arguments -x64"}
  if ($selectedRuntime) {$arguments = "$arguments -runtime $selectedRuntime"}
  if ($persistent) {$arguments = "$arguments -persistent"}
  if ($force) {$arguments = "$arguments -force"}
  if (!$(String-IsEmptyOrWhitespace($alias))) {$arguments = "$arguments -alias '$alias'"}
  return $arguments
}

function Validate-And-Santitise-Switches()
{
  if ($svr50 -and $runtime) {throw "You cannot select both the -runtime switch and the -svr50 runtimes"}
  if ($svrc50 -and $runtime) {throw "You cannot select both the -runtime switch and the -svrc50 runtimes"}
  if ($x86 -and $amd64) {throw "You cannot select both x86 and amd64 architectures"}
  if ($x86 -and $x64) {throw "You cannot select both x86 and x64 architectures"}
  if ($x64 -and $amd64) {throw "You cannot select both x64 and amd64 architectures"}

  if ($runtime) {
    $validRuntimes = "CoreCLR", "CLR", "svr50", "svrc50"
    $match = $validRuntimes | ? { $_ -like $runtime } | Select -First 1
    if (!$match) {throw "'$runtime' is not a valid runtime"}
    Set-Variable -Name "selectedRuntime" -Value $match -Scope Script
  } elseif ($svr50) {
    Write-Host "Warning: -svr50 is deprecated, use -runtime CLR for new packages."
    Set-Variable -Name "selectedRuntime" -Value "svr50" -Scope Script
  } elseif ($svrc50) {
    Write-Host "Warning: -svrc50 is deprecated, use -runtime CoreCLR for new packages."
    Set-Variable -Name "selectedRuntime" -Value "svrc50" -Scope Script
  }

  if ($x64) {
    Write-Host "Warning: -x64 is deprecated, use -amd64 for new packages."
    Set-Variable -Name "selectedArch" -Value "x64" -Scope Script
  } elseif ($amd64) {
    Set-Variable -Name "selectedArch" -Value "amd64" -Scope Script
  } elseif ($x86) {
    Set-Variable -Name "selectedArch" -Value "x86" -Scope Script
  }
}

try {
  Validate-And-Santitise-Switches
  if ($global) {
    switch -wildcard ($command + " " + $args.Count) {
      "setup 0"           {Kvm-Global-Setup}
      "upgrade 0"         {Kvm-Global-Upgrade}
      "install 1"         {Kvm-Install $args[0] $true}
      "use 1"             {Kvm-Global-Use $args[0]}
      default             {throw "Unknown command, or global switch not supported"};
    }
  } else {
    switch -wildcard ($command + " " + $args.Count) {
      "setup 0"           {Kvm-Global-Setup}
      "upgrade 0"         {Kvm-Upgrade}
      "install 1"         {Kvm-Install $args[0] $false}
      "list 0"            {Kvm-List}
      "use 1"             {Kvm-Use $args[0]}
      "alias 0"           {Kvm-Alias-List}
      "alias 1"           {Kvm-Alias-Get $args[0]}
      "alias 2"           {Kvm-Alias-Set $args[0] $args[1]}
      "unalias 1"         {Kvm-Unalias $args[0]}
      "help 0"            {Kvm-Help}
      " 0"                {Kvm-Help}
      default             {throw "Unknown command"};
    }
  }
}
catch {
  Write-Host $_ -ForegroundColor Red ;
  Write-Host "Type 'kvm help' for help on how to use kvm."
}
if ($wait) {
  Write-Host "Press any key to continue ..."
  $x = $host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown,AllowCtrlC")
}
