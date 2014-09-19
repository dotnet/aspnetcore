param(
  [parameter(Position=0)]
  [string] $command,
  [string] $proxy,
  [switch] $verbosity = $false,
  [alias("g")][switch] $global = $false,
  [alias("p")][switch] $persistent = $false,
  [alias("f")][switch] $force = $false,
  [switch] $x86 = $false,
  [switch] $x64 = $false,
  [switch] $svr50 = $false,
  [switch] $svrc50 = $false,
  [alias("a")]
  [string] $alias = $null,
  [parameter(Position=1, ValueFromRemainingArguments=$true)]
  [string[]]$args=@()
)

$userKrePath = $env:USERPROFILE + "\.kre"
$userKrePackages = $userKrePath + "\packages"
$globalKrePath = $env:ProgramFiles + "\KRE"
$globalKrePackages = $globalKrePath + "\packages"
$feed = $env:KRE_NUGET_API_URL

if (!$feed)
{
    $feed = "https://www.myget.org/F/aspnetrelease/api/v2";
}

$scriptPath = $myInvocation.MyCommand.Definition

function Kvm-Help {
@"
K Runtime Environment Version Manager - Build 10002

USAGE: kvm <command> [options]

kvm upgrade [-x86][-x64] [-svr50][-svrc50] [-g|-global] [-proxy <ADDRESS>]
  install latest KRE from feed
  set 'default' alias to installed version
  add KRE bin to user PATH environment variable
  -g|-global        install to machine-wide location
  -f|-force         upgrade even if latest is already installed
  -proxy <ADDRESS>  use given address as proxy when accessing remote server

kvm install <semver>|<alias>|<nupkg>|latest [-x86][-x64] [-svr50][-svrc50] [-a|-alias <alias>] [-g|-global] [-f|-force]
  <semver>|<alias>  install requested KRE from feed
  <nupkg>           install requested KRE from package on local filesystem
  latest            install latest KRE from feed
  add KRE bin to path of current command line
  -p|-persistent    add KRE bin to PATH environment variables persistently
  -a|-alias <alias> set alias <alias> for requested KRE on install
  -g|-global        install to machine-wide location
  -f|-force         install even if specified version is already installed

kvm use <semver>|<alias>|none [-x86][-x64] [-svr50][-svrc50] [-p|-persistent] [-g|-global]
  <semver>|<alias>  add KRE bin to path of current command line
  none              remove KRE bin from path of current command line
  -p|-persistent    add KRE bin to PATH environment variables persistently
  -g|-global        combined with -p to change machine PATH instead of user PATH

kvm list
  list KRE versions installed

kvm alias
  list KRE aliases which have been defined

kvm alias <alias>
  display value of named alias

kvm alias <alias> <semver>|<alias> [-x86][-x64] [-svr50][-svrc50]
  <alias>            The name of the alias to set
  <semver>|<alias>   The KRE version to set the alias to. Alternatively use the version of the specified alias

"@ | Write-Host
}

function Kvm-Global-Setup {
    If (Needs-Elevation)
    {
        $arguments = "& '$scriptPath' setup $(Requested-Switches) -persistent"
        Start-Process "$psHome\powershell.exe" -Verb runAs -ArgumentList $arguments -Wait
        Write-Host "Setup complete"
        Kvm-Help
        break
    }

    $scriptFolder = [System.IO.Path]::GetDirectoryName($scriptPath)

    $kvmBinPath = "$userKrePath\bin"

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

    Write-Host "Press any key to continue ..."
    $x = $host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown,AllowCtrlC")
}

function Kvm-Global-Upgrade {
    $persistent = $true
    $alias="default"
    If (Needs-Elevation) {
        $arguments = "& '$scriptPath' upgrade -global $(Requested-Switches)"
        Start-Process "$psHome\powershell.exe" -Verb runAs -ArgumentList $arguments -Wait
        break
    }
    Kvm-Global-Install "latest"
}

function Kvm-Upgrade {
    $persistent = $true
    $alias="default"
    Kvm-Install "latest"
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

function Kvm-Global-Install {
param(
  [string] $versionOrAlias
)
    If (Needs-Elevation) {
        $arguments = "& '$scriptPath' install -global $versionOrAlias $(Requested-Switches)"
        Start-Process "$psHome\powershell.exe" -Verb runAs -ArgumentList $arguments -Wait
        Kvm-Global-Use $versionOrAlias
        break
    }

    if ($versionOrAlias -eq "latest") {
        $versionOrAlias = Kvm-Find-Latest (Requested-Platform "svr50") (Requested-Architecture "x86")
    }

    $kreFullName = Requested-VersionOrAlias $versionOrAlias

    Do-Kvm-Download $kreFullName $globalKrePackages
    Kvm-Use $versionOrAlias
    if (![string]::IsNullOrWhiteSpace($alias)) {
        Kvm-Alias-Set $alias $versionOrAlias
    }
}

function Kvm-Install {
param(
  [string] $versionOrAlias
)
    if ($versionOrAlias.EndsWith(".nupkg"))
    {
        $kreFullName = [System.IO.Path]::GetFileNameWithoutExtension($versionOrAlias)
        $kreFolder = "$userKrePackages\$kreFullName"
        $folderExists = Test-Path $kreFolder

        if($folderExists -and $force) {
            del $kreFolder -Recurse -Force
            $folderExists = $false;
        }

        if($folderExists) {
            Write-Host "Target folder '$kreFolder' already exists"
        } else {
            $tempUnpackFolder = Join-Path $userKrePackages "temp"
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
        }

        $packageVersion = Package-Version $kreFullName
        Kvm-Use $packageVersion
        if (![string]::IsNullOrWhiteSpace($alias)) {
            Kvm-Alias-Set $alias $packageVersion
        }
    }
    else
    {
        if ($versionOrAlias -eq "latest") {
            $versionOrAlias = Kvm-Find-Latest (Requested-Platform "svr50") (Requested-Architecture "x86")
        }
        $kreFullName = Requested-VersionOrAlias $versionOrAlias

        Do-Kvm-Download $kreFullName $userKrePackages
        Kvm-Use $versionOrAlias
        if (![string]::IsNullOrWhiteSpace($alias)) {
            Kvm-Alias-Set $alias $versionOrAlias
        }
    }
}

function Kvm-List {
  $kreHome = $env:KRE_HOME
  if (!$kreHome) {
    $kreHome = $env:ProgramFiles + "\KRE;%USERPROFILE%\.kre"
  }
  $items = @()
  foreach($portion in $kreHome.Split(';')) {
    $path = [System.Environment]::ExpandEnvironmentVariables($portion)
    if (Test-Path("$path\packages")) {
      $items += Get-ChildItem ("$path\packages\KRE-*") | List-Parts
    }
  }
  $items | Sort-Object Version, Runtime, Architecture | Format-Table -AutoSize -Property @{name="Active";expression={$_.Active};alignment="center"}, "Version", "Runtime", "Architecture", "Location"
}

filter List-Parts {
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
  $parts1 = $_.Name.Split('.', 2)
  $parts2 = $parts1[0].Split('-', 3)
  return New-Object PSObject -Property @{
    Active = if($active){"*"}else{""}
    Version = $parts1[1]
    Runtime = $parts2[1]
    Architecture = $parts2[2]
    Location = $_.Parent.FullName
  }
}

function Kvm-Global-Use {
param(
  [string] $versionOrAlias
)
    If (Needs-Elevation) {
        $arguments = "& '$scriptPath' use -global $versionOrAlias $(Requested-Switches)"
        if ($persistent) {
          $arguments = $arguments + " -persistent"
        }
        Start-Process "$psHome\powershell.exe" -Verb runAs -ArgumentList $arguments -Wait
        break
    }

    if ($versionOrAlias -eq "none") {
      Write-Host "Removing KRE from process PATH"
      Set-Path (Change-Path $env:Path "" ($globalKrePackages, $userKrePackages))

      if ($persistent) {
          Write-Host "Removing KRE from machine PATH"
          $machinePath = [Environment]::GetEnvironmentVariable("Path", [System.EnvironmentVariableTarget]::Machine)
          $machinePath = Change-Path $machinePath "" ($globalKrePackages, $userKrePackages)
          [Environment]::SetEnvironmentVariable("Path", $machinePath, [System.EnvironmentVariableTarget]::Machine)
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
        Write-Host "Adding $kreBin to machine PATH"
        $machinePath = [Environment]::GetEnvironmentVariable("Path", [System.EnvironmentVariableTarget]::Machine)
        $machinePath = Change-Path $machinePath $kreBin ($globalKrePackages, $userKrePackages)
        [Environment]::SetEnvironmentVariable("Path", $machinePath, [System.EnvironmentVariableTarget]::Machine)
    }
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

    Write-Host "Setting alias '$name' to '$kreFullName'"
    md ($userKrePath + "\alias\") -Force | Out-Null
    $kreFullName | Out-File ($userKrePath + "\alias\" + $name + ".txt") ascii
}

function Locate-KreBinFromFullName() {
param(
  [string] $kreFullName
)
  $kreHome = $env:KRE_HOME
  if (!$kreHome) {
    $kreHome = $env:ProgramFiles + ";%USERPROFILE%\.kre"
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
        $pkgPlatform = Requested-Platform "svr50"
        $pkgArchitecture = Requested-Architecture "x86"
    }
    return "KRE-" + $pkgPlatform + "-" + $pkgArchitecture + "." + $pkgVersion
}

function Requested-Platform() {
param(
  [string] $default
)
    if ($svr50 -and $svrc50) {
        Throw "This command cannot accept both -svr50 and -svrc50"
    }
    if ($svr50) {
        return "svr50"
    }
    if ($svrc50) {
        return "svrc50"
    }
    return $default
}

function Requested-Architecture() {
param(
  [string] $default
)
    if ($x86 -and $x64) {
        Throw "This command cannot accept both -x86 and -x64"
    }
    if ($x86) {
        return "x86"
    }
    if ($x64) {
        return "x64"
    }
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
  if ($x64) {$arguments = "$arguments -x64"}
  if ($svr50) {$arguments = "$arguments -svr50"}
  if ($svrc50) {$arguments = "$arguments -svrc50"}
  return $arguments
}

 try {
   if ($global) {
    switch -wildcard ($command + " " + $args.Count) {
      "setup 0"           {Kvm-Global-Setup}
      "upgrade 0"         {Kvm-Global-Upgrade}
      "install 1"         {Kvm-Global-Install $args[0]}
#      "list 0"            {Kvm-Global-List}
      "use 1"             {Kvm-Global-Use $args[0]}
      default             {Write-Host 'Unknown command, or global switch not supported'; Kvm-Help;}
    }
   } else {
    switch -wildcard ($command + " " + $args.Count) {
      "setup 0"           {Kvm-Global-Setup}
      "upgrade 0"         {Kvm-Upgrade}
      "install 1"         {Kvm-Install $args[0]}
      "list 0"            {Kvm-List}
      "use 1"             {Kvm-Use $args[0]}
      "alias 0"           {Kvm-Alias-List}
      "alias 1"           {Kvm-Alias-Get $args[0]}
      "alias 2"           {Kvm-Alias-Set $args[0] $args[1]}
      "help 0"              {Kvm-Help}
      " 0"              {Kvm-Help}
      default             {Write-Host 'Unknown command'; Kvm-Help;}
    }
   }
  }
  catch {
    Write-Host $_ -ForegroundColor Red ;
  }
