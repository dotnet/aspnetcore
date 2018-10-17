<#
.SYNOPSIS
Restarts the TC build agent service without rebooting the machine
#>
[CmdletBinding(DefaultParameterSetName = 'Default')]
param(
    [Parameter(ParameterSetName = 'Default')]
    [string]$Account = "REDMOND\asplab",
    [Parameter(ParameterSetName = 'Credential')]
    [pscredential]$Credential,
    [string[]]$Agents = $null
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 1
Import-Module -Scope Local -Force $PSScriptRoot/agentlist.psm1

if (-not $Agents) {
    $Agents = Get-Agents | ? { ($_.OS -eq 'windows') -and ($_.Category -ne 'Codesign') } | % { $_.Name }
}

if (-not $Credential) {
    $Credential = Get-Credential -UserName $Account -Message 'Enter password used to login to agents'
}

foreach ($agent in $Agents) {
    
    $session = New-PSSession -ComputerName $agent -Credential $Credential

    Write-Host "Restarting service on $agent"
    
    Invoke-Command -Session $session -ScriptBlock {
        $ErrorActionPreference = 'Stop'
        Set-StrictMode -Version 1

        $Service = Get-WmiObject -Class Win32_Service -Filter  "name like '%TCBuildAgent%'"

        Write-Host "Stopping the service..."
        $result = $Service.StopService().ReturnValue
            
        if ($result -ne '0') {
            Write-Error "Failed to stop the service. Result code: $result"
            Write-Host "Trying to start the service anyway as earlier error could be because the service was already in stopped state"
        }
            
        # Try to sleep only if stopping the service call succeeded
        if ($result -eq '0') {
            $sleepTimeInSeconds = 10
            Write-Host "Sleeping for '$sleepTimeInSeconds' seconds to enable the service to stop completely"
            Start-Sleep -Seconds $sleepTimeInSeconds
        }
        
        Write-Host "Starting the service..."
        $result = $Service.StartService().ReturnValue
        
        if ($result -eq '0') {
            Write-Host "Service started successfully." -ForegroundColor Green
        }
        else {
            Write-Error "Failed to start the service. Result code: $result"
        }
    }

    Remove-PSSession $session
}
