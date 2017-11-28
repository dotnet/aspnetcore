<#
.SYNOPSIS
Installs or updates Visual Studio on build agents
#>
[CmdletBinding()]
param(
    [string]$account = "REDMOND\asplab"
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 1
Import-Module -Scope Local -Force $PSScriptRoot/agentlist.psm1

$intermedateDir = "$PSScriptRoot/obj/"
mkdir $intermedateDir -ErrorAction Ignore | Out-Null

$bootstrapper = "$intermedateDir/vs_enterprise.exe"

if (-not (Test-Path $bootstrapper)) {
    Invoke-WebRequest -Uri 'https://aka.ms/vs/15/release/vs_enterprise.exe' -OutFile $bootstrapper
}

$agents = Get-Agents | ? { ($_.OS -eq 'windows') -and ($_.Category -ne 'Codesign') } | % { $_.Name }

$credential = Get-Credential -UserName $account -Message 'Enter password used to login to agents'

foreach ($agent in $agents) {
    
    $session = New-PSSession -ComputerName $agents -Credential $credential

    Write-Host "Installing or updating VS on $agent"
    
    Invoke-Command -Session $session -ScriptBlock {
        Remove-Item "C:/temp/vs_install" -Recurse -Force -ErrorAction Ignore | Out-Null
        mkdir "C:/temp/vs_install" -ErrorAction Ignore | Out-Null
    }
    
    Copy-Item -ToSession $session $bootstrapper 'C:/temp/vs_install/vs_enterprise.exe'
    Copy-Item -ToSession $session $PSScriptRoot/vs.agents.json 'C:/temp/vs_install/vs.agents.json' 
    
    Invoke-Command -Session $session -ScriptBlock {
        $ErrorActionPreference = 'Stop'
        Set-StrictMode -Version 1
        $vsInstallPath = "${env:ProgramFiles(x86)}/Microsoft Visual Studio/2017/Enterprise/"
        $verb = if (Test-Path $vsInstallPath) {
            'update'
        }
        else {
            'install'
        }
        
        try {
            Write-Host "Running 'vs_enterprise.exe $verb' on $(hostname)"
            Start-Process -FilePath 'C:/temp/vs_install/vs_enterprise.exe' `
                -ArgumentList @(
                $verb,
                '--installPath', "`"$vsInstallPath`"",
                '--in', 'C:/temp/vs_install/vs.agents.json',
                '--quiet',
                '--wait',
                '--norestart') `
                -Wait `
                -Verb runas `
                -ErrorAction Stop
        }
        finally {
            Remove-Item "C:/temp/vs_install" -Recurse -Force -ErrorAction SilentlyContinue
        }
    }
}