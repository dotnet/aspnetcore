<#
.SYNOPSIS
    Installs or updates Visual Studio on build agents
.PARAMETER Account
    User name to use to remote into agents
.PARAMETER Credential
    Login
.PARAMETER Agents
    A list of computer names to install VS onto. If not specified, the list is pulled from agentlist.psm1
.PARAMETER ExcludeAgents
    A list of computer names to skip.
.PARAMETER Update
    Update VS to latest version instead of modifying the installation to include new workloads.
#>
[CmdletBinding(DefaultParameterSetName = 'Default')]
param(
    [Parameter(ParameterSetName = 'Default')]
    [string]$Account = $null,
    [Parameter(ParameterSetName = 'Credential')]
    [pscredential]$Credential,
    # A list of
    [string[]]$Agents = $null,
    # Skip agents
    [string[]]$ExcludeAgents = $null,
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
    $Agents = Get-Agents | ? { $_.OS -eq 'windows' } | % { $_.Name }
}

if (-not $Account) {
    $Account = whoami
}

if (-not $Credential) {
    $Credential = Get-Credential -UserName $Account -Message 'Enter password used to login to agents'
}

foreach ($agent in $Agents) {

    if ($ExcludeAgents -contains $agent) {
        Write-Host -ForegroundColor Yellow "Skipping $agent"
        continue
    }

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
            # prepend the arg list with the verb
            if ($using:Update) {
                $arguments = ,'update' + $arguments
            }
            else {
                $arguments = ,'modify' + $arguments
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

            # https://docs.microsoft.com/en-us/visualstudio/install/use-command-line-parameters-to-install-visual-studio#error-codes
            if ($process.ExitCode -eq 3010) {
                Write-Warning "Agent $(hostname) requires restart to finish the VS update"
            }
            elseif ($process.ExitCode -eq 5007) {
                Write-Error "Operation was blocked - the computer does not meet the requirements"
            }
            elseif (($process.ExitCode -eq 5004) -or ($process.ExitCode -eq 1602)) {
                Write-Error "Operation was canceled"
            }
            elseif ($process.ExitCode -ne 0) {
                Write-Error "Installation failed on $(hostname) for unknown reason"
            }
        }
        finally {
            Remove-Item $using:tempItemDir -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    Remove-PSSession $session
}
