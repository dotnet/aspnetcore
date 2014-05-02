param(
  [parameter(Position=0)]
  [string] $command,
  [switch] $verbosity = $false,
  [alias("g")][switch] $global = $false,
  [switch] $x86 = $false,
  [switch] $x64 = $false,
  [switch] $svr50 = $false,
  [switch] $svrc50 = $false,
  [parameter(Position=1, ValueFromRemainingArguments=$true)]
  [string[]]$args=@()
)

$userKrePath = $env:USERPROFILE + "\.kre"
$globalKrePath = $env:ProgramFiles + "\KRE"

$scriptPath = $myInvocation.MyCommand.Definition

function Kvm-Help {
@"
kvm - K Runtime Environment Version Manager

kvm upgrade
  install latest KRE from feed
  set 'default' alias
  add KRE bin to path of current command line

kvm install <semver>|<alias> [-x86][-x64] [-svr50][-svrc50] [-g|-global]
  install requested KRE from feed

kvm list [-g|-global]
  list KRE versions installed 

kvm use <semver>|<alias> [-x86][-x64] [-svr50][-svrc50] [-g|-global]
  add KRE bin to path of current command line

kvm alias
  list KRE aliases which have been defined

kvm alias <alias>
  display value of named alias

kvm alias <alias> <semver> [-x86][-x64] [-svr50][-svrc50]
  set alias to specific version

"@ | Write-Host
}


function Kvm-Upgrade {
    $version = Kvm-Find-Latest (Requested-Platform "svr50") (Requested-Architecture "x86")
    Kvm-Install $version
    Kvm-Alias-Set "default" $version
}


function Kvm-Find-Latest {
param(
    [string] $platform,
    [string] $architecture
)
    Write-Host "Determining latest version"

    $url = "https://www.myget.org/F/aspnetvnext/api/v2/GetUpdates()?packageIds=%27KRE-$platform-$architecture%27&versions=%270.0%27&includePrerelease=true&includeAllVersions=false"

    $wc = New-Object System.Net.WebClient
    $wc.Credentials = new-object System.Net.NetworkCredential("aspnetreadonly", "4d8a2d9c-7b80-4162-9978-47e918c9658c")
    [xml]$xml = $wc.DownloadString($url)

    $version = Select-Xml "//d:Version" -Namespace @{d='http://schemas.microsoft.com/ado/2007/08/dataservices'} $xml 

    return $version
}

function Kvm-Install-Latest {
    Kvm-Install (Kvm-Find-Latest (Requested-Platform "svr50") (Requested-Architecture "x86"))
}

function Do-Kvm-Download {
param(
  [string] $kreFullName,
  [string] $kreFolder
)
    $parts = $kreFullName.Split(".", 2)

    $url = "https://www.myget.org/F/aspnetvnext/api/v2/package/" + $parts[0] + "/" + $parts[1]
    $kreFile = "$kreFolder\$kreFullName.nupkg"

    If (Test-Path $kreFolder) {
        Remove-Item $kreFolder -Force -Recurse
    }

    Write-Host "Downloading" $kreFullName "from https://www.myget.org/F/aspnetvnext/api/v2/"

    md $kreFolder -Force | Out-Null

    $wc = New-Object System.Net.WebClient
    $wc.Credentials = new-object System.Net.NetworkCredential("aspnetreadonly", "4d8a2d9c-7b80-4162-9978-47e918c9658c")
    $wc.DownloadFile($url, $kreFile)

    Do-Kvm-Unpack $kreFile $kreFolder
}

function Do-Kvm-Unpack {
param(
  [string] $kreFile,
  [string] $kreFolder
)
    Write-Host "Installing to" $kreFolder

    [System.Reflection.Assembly]::LoadWithPartialName('System.IO.Compression.FileSystem') | Out-Null
    [System.IO.Compression.ZipFile]::ExtractToDirectory($kreFile, $kreFolder)

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
  [string] $versionOrAlias
)
    if ($versionOrAlias.EndsWith(".nupkg"))
    {
        $kreFullName = [System.IO.Path]::GetFileNameWithoutExtension($versionOrAlias)
        $kreFolder = "$userKrePath\packages\$kreFullName"
        $kreFile = "$kreFolder\$kreFullName.nupkg"

        md $kreFolder -Force | Out-Null

        copy $versionOrAlias $kreFile

        Do-Kvm-Unpack $kreFile $kreFolder
    }
    else
    {
        $kreFullName = Requested-VersionOrAlias $versionOrAlias

        $kreFolder = "$userKrePath\packages\$kreFullName"

        Do-Kvm-Download $kreFullName $kreFolder
        Kvm-Use $versionOrAlias
    }
}

function Kvm-Global-Install {
param(
  [string] $versionOrAlias
)
    If (-NOT ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator"))
    {
        $arguments = "install -global $versionOrAlias" 
        if ($x86) {$arguments = "$arguments -x86"}
        if ($x64) {$arguments = "$arguments -x64"}
        if ($svr50) {$arguments = "$arguments -svr50"}
        if ($svrc50) {$arguments = "$arguments -svrc50"}

        $arguments = "& '$scriptPath' $arguments"
        Start-Process "$psHome\powershell.exe" -Verb runAs -ArgumentList $arguments
        Kvm-Global-Use $versionOrAlias
        break
    }

    $kreFullName = Requested-VersionOrAlias $versionOrAlias

    $kreFolder = $globalKrePath + "\packages\$kreFullName"

    Do-Kvm-Download $kreFullName $kreFolder
}

function Kvm-List {
    Get-ChildItem ($userKrePath + "\packages\") | Select Name
}

function Kvm-Global-List {
    Get-ChildItem ($globalKrePath + "\packages\") | Select Name
}

function Kvm-Use {
param(
  [string] $versionOrAlias
)
    $kreFullName = Requested-VersionOrAlias $versionOrAlias

    $kreBin = $userKrePath + "\packages\" + $kreFullName + "\bin"

    Write-Host "Adding" $kreBin "to PATH"

    $newPath = $kreBin
    foreach($portion in $env:Path.Split(';')) {
      if (!$portion.StartsWith($userKrePath) -and !$portion.StartsWith($globalKrePath)) {
        $newPath = $newPath + ";" + $portion
      }
    }

@"
SET "KRE_VERSION=$version"
SET "PATH=$newPath"
"@ | Out-File ($userKrePath + "\run-once.cmd") ascii
}

function Kvm-Global-Use {
param(
  [string] $versionOrAlias
)
    $kreFullName = Requested-VersionOrAlias $versionOrAlias

    $kreBin = "$globalKrePath\packages\$kreFullName\bin"

    Write-Host "Adding" $kreBin "to PATH"

    $newPath = $kreBin
    foreach($portion in $env:Path.Split(';')) {
      if (!$portion.StartsWith($userKrePath) -and !$portion.StartsWith($globalKrePath)) {
        $newPath = $newPath + ";" + $portion
      }
    }

@"
SET "KRE_VERSION=$version"
SET "PATH=$newPath"
"@ | Out-File ($userKrePath + "\run-once.cmd") ascii
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
    Write-Host "Alias '$name' is set to" (Get-Content ($userKrePath + "\alias\" + $name + ".txt"))
}

function Kvm-Alias-Set {
param(
  [string] $name,
  [string] $value
)
    $kreFullName = "KRE-" + (Requested-Platform "svr50") + "-" + (Requested-Architecture "x86") + "." + $value

    Write-Host "Setting alias '$name' to '$kreFullName'"
    md ($userKrePath + "\alias\") -Force | Out-Null
    $kreFullName | Out-File ($userKrePath + "\alias\" + $name + ".txt") ascii
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


 try {
   if ($global) {
    switch -wildcard ($command + " " + $args.Count) {
#      "upgrade 0"         {Kvm-Global-Upgrade}
#      "install 0"         {Kvm-Global-Install-Latest}
      "install 1"         {Kvm-Global-Install $args[0]}
      "list 0"            {Kvm-Global-List}
      "use 1"             {Kvm-Global-Use $args[0]}
      default             {Write-Host 'Unknown command, or global switch not supported'; Kvm-Help;}
    }
   } else {
    switch -wildcard ($command + " " + $args.Count) {
      "upgrade 0"         {Kvm-Upgrade}
      "install 0"         {Kvm-Install-Latest}
      "install 1"         {Kvm-Install $args[0]}
      "list 0"            {Kvm-List}
      "use 1"             {Kvm-Use $args[0]}
      "alias 0"           {Kvm-Alias-List}
      "alias 1"           {Kvm-Alias-Get $args[0]}
      "alias 2"           {Kvm-Alias-Set $args[0] $args[1]}
      "help"              {Kvm-Help}
      default             {Write-Host 'Unknown command'; Kvm-Help;}
    }
   }
  }
  catch {
    Write-Host $_ -ForegroundColor Red ;
  }
