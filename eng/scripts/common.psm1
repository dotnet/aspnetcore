$ErrorActionPreference = 'Stop'
# Update the default TLS support to 1.2
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

function Invoke-Block([scriptblock]$cmd, [string]$WorkingDir = $null) {
    if ($WorkingDir) {
        Push-Location $WorkingDir
    }

    try {

        $cmd | Out-String | Write-Verbose
        & $cmd

        # Need to check both of these cases for errors as they represent different items
        # - $?: did the powershell script block throw an error
        # - $lastexitcode: did a windows command executed by the script block end in error
        if ((-not $?) -or ($lastexitcode -ne 0)) {
            if ($error -ne $null)
            {
                Write-Warning $error[0]
            }
            throw "Command failed to execute: $cmd"
        }
    }
    finally {
        if ($WorkingDir) {
            Pop-Location
        }
    }
}

function SaveXml([xml]$xml, [string]$path) {
    Write-Verbose "Saving to $path"
    $ErrorActionPreference = 'stop'

    $settings = New-Object System.XML.XmlWriterSettings
    $settings.OmitXmlDeclaration = $true
    $settings.Encoding = New-Object System.Text.UTF8Encoding( $true )
    $writer = [System.XML.XMLTextWriter]::Create($path, $settings)
    $xml.Save($writer)
    $writer.Close()
}

function LoadXml([string]$path) {
    Write-Verbose "Reading from $path"

    $ErrorActionPreference = 'stop'
    $obj = new-object xml
    $obj.PreserveWhitespace = $true
    $obj.Load($path)
    return $obj
}

function Get-MSBuildPath {
    param(
        [switch]$Prerelease,
        [string[]]$Requires
    )

    $vsInstallDir = $null
    if ($env:VSINSTALLDIR -and (Test-Path $env:VSINSTALLDIR)) {
        $vsInstallDir = $env:VSINSTALLDIR
        Write-Verbose "Using VSINSTALLDIR=$vsInstallDir"
    }
    else {
        $vswhere = "${env:ProgramFiles(x86)}/Microsoft Visual Studio/Installer/vswhere.exe"
        Write-Verbose "Using vswhere.exe from $vswhere"

        if (-not (Test-Path $vswhere)) {
            Write-Error "Missing prerequisite: could not find vswhere"
        }

        [string[]] $vswhereArgs = @()

        if ($Prerelease) {
            $vswhereArgs += '-prerelease'
        }

        if ($Requires) {
            foreach ($r in $Requires) {
                $vswhereArgs += '-requires', $r
            }
        }

        $installs = & $vswhere -format json -version '[15.0, 16.0)' -latest -products * @vswhereArgs | ConvertFrom-Json
        if (!$installs) {
            Write-Error "Missing prerequisite: could not find any installations of Visual Studio"
        }

        $vs = $installs | Select-Object -First 1
        $vsInstallDir = $vs.installationPath
        Write-Host "Using $($vs.displayName)"
    }

    $msbuild = Join-Path  $vsInstallDir 'MSBuild/15.0/bin/msbuild.exe'
    if (!(Test-Path $msbuild)) {
        Write-Error "Missing prerequisite: could not find msbuild.exe"
    }
    return $msbuild
}

function Get-RemoteFile([string]$RemotePath, [string]$LocalPath) {
    if ($RemotePath -notlike 'http*') {
        Copy-Item $RemotePath $LocalPath
        return
    }

    $retries = 10
    while ($retries -gt 0) {
        $retries -= 1
        try {
            Invoke-WebRequest -UseBasicParsing -Uri $RemotePath -OutFile $LocalPath
            return
        }
        catch {
            Write-Verbose "Request failed. $retries retries remaining"
        }
    }

    Write-Error "Download failed: '$RemotePath'."
}
