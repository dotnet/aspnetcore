[CmdletBinding()]
param(
    [string]$buildAgentName=$env:COMPUTERNAME,

    [string]$teamAgentServiceAccountName="redmond\asplab",

    [Parameter(Mandatory=$true)]
    [string]$teamAgentServiceAccountPassword,

    [string]$setupFilesShare="\\aspnetci\share\BuildAgentSetupFiles"
)

$buildAgentFolder="C:\BuildAgent"

Write-Host "`nUpdating build agent name..."
$agentPropertiesFile="$buildAgentFolder\conf\buildAgent.properties"
(Get-Content $agentPropertiesFile).replace('#AGENT_NAME#', $buildAgentName) | Set-Content $agentPropertiesFile

Write-Host "`nUpdating TeamCity service account password configuration value and Starting the team city agent service..."
.\BuildAgentPasswordChange.ps1 -buildAgentFolder $buildAgentFolder -teamAgentServiceAccountName $teamAgentServiceAccountName -teamAgentServiceAccountPassword $teamAgentServiceAccountPassword
