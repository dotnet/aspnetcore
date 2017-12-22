<#
.SYNOPSIS
Installs or updates Visual Studio on build agents
#>
[CmdletBinding(DefaultParameterSetName = 'Default')]
param(
    [Parameter(ParameterSetName = 'Default')]
    [string]$Account = "REDMOND\asplab",
    [Parameter(ParameterSetName = 'Credential')]
    [pscredential]$Credential,
    [string[]]$Agents = $null,
    [switch]$Update
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 1
Import-Module -Scope Local -Force $PSScriptRoot/agentlist.psm1

$intermedateDir = "$PSScriptRoot\obj\"
mkdir $intermedateDir -ErrorAction Ignore | Out-Null

$bootstrapper = "$intermedateDir\vs_enterprise.exe"
Invoke-WebRequest -Uri 'https://aka.ms/vs/15/pre/vs_enterprise.exe' -OutFile $bootstrapper

if (-not $Agents) {
    $Agents = Get-Agents | ? { ($_.OS -eq 'windows') -and ($_.Category -ne 'Codesign') } | % { $_.Name }
}

if (-not $Credential) {
    $Credential = Get-Credential -UserName $Account -Message 'Enter password used to login to agents'
}

foreach ($agent in $Agents) {
    
    $session = New-PSSession -ComputerName $agent -Credential $Credential

    Write-Host "Installing or updating VS on $agent"

    $tempItemDir = 'C:\temp\vs_install'
    $rspFile = Join-Path $tempItemDir 'vs.agents.json'
    $installerPath = Join-Path $tempItemDir 'vs_enterprise.exe'
    
    # $using: is a special PowerShell 5 syntax for passing variables to a remote command

    Invoke-Command -Session $session -ScriptBlock {
        Remove-Item $using:tempItemDir -Recurse -Force -ErrorAction Ignore | Out-Null
        mkdir $using:tempItemDir -ErrorAction Ignore | Out-Null
    }
    
    Copy-Item -ToSession $session $bootstrapper $installerPath
    Copy-Item -ToSession $session $PSScriptRoot\vs.agents.json $rspFile
    
    Invoke-Command -Session $session -ScriptBlock {
        $ErrorActionPreference = 'Stop'
        Set-StrictMode -Version 1
        # no backslashes - this breaks the installer
        $vsInstallPath = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\aspnetci\Enterprise"
        $arguments = @(
            '--installPath', "`"$vsInstallPath`"",
            '--in', $using:rspFile,
            '--quiet',
            '--nickname', 'aspnetci',
            '--wait',
            '--norestart')

        if (Test-Path $vsInstallPath) {
            if ($using:Update) {
                $arguments += 'update'
            }
            else {
                $arguments += 'modify'
            }
        }

        try {
            Write-Host "Running 'vs_enterprise.exe $arguments' on $(hostname)"
            $process = Start-Process -FilePath $using:installerPath `
                -ArgumentList $arguments `
                -Verb runas `
                -PassThru `
                -ErrorAction Stop
            Write-Host "pid = $($process.Id)"
            Wait-Process -InputObject $process
            Write-Host "exit code = $($process.ExitCode)"
            if ($process.ExitCode -ne 0) {
                Write-Error "Installation failed on $(hostname)"
            }
        }
        finally {
            Remove-Item $using:tempItemDir -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    Remove-PSSession $session
}