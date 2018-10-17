<#
.SYNOPSIS
    Runs an artibrary script block on all Windows CI agents
.PARAMETER Account
    User name to use to remote into agents
.PARAMETER Credential
    Login
.PARAMETER Agents
    A list of computer names to invoke the command on. If not specified, the list is pulled from agentlist.psm1
.PARAMETER ExcludeAgents
    A list of computer names to skip.
.PARAMETER ScriptBlock
    The script block to execute remotely
#>
[CmdletBinding(DefaultParameterSetName = 'Default')]
param(
    [Parameter(ParameterSetName = 'Default')]
    [string]$Account = "REDMOND\asplab",
    [Parameter(ParameterSetName = 'Credential')]
    [pscredential]$Credential,
    # A list of
    [string[]]$Agents = $null,
    # Skip agents
    [string[]]$ExcludeAgents = $null,
    [Parameter(Mandatory = $true)]
    [scriptblock]$ScriptBlock
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 1
Import-Module -Scope Local -Force $PSScriptRoot/agentlist.psm1

if (-not $Agents) {
    $Agents = Get-Agents | ? { $_.OS -eq 'windows' } | % { $_.Name }
}

if (-not $Credential) {
    $Credential = Get-Credential -UserName $Account -Message 'Enter password used to login to agents'
}

foreach ($agent in $Agents) {

    if ($ExcludeAgents -contains $agent) {
        Write-Host -ForegroundColor Yellow "Skipping $agent"
        continue
    }

    Write-Host "Running script on $agent"

    $session = New-PSSession -ComputerName $agent -Credential $Credential

    Invoke-Command -Session $session -ScriptBlock $ScriptBlock

    Remove-PSSession $session
}
