[CmdletBinding()]
param(
    [string]$teamAgentServiceAccountName="redmond\asplab",

    [Parameter(Mandatory=$true)]
    [string]$teamAgentServiceAccountPassword,
    [string[]]$Agents
)

Import-Module -Scope Local -Force $PSScriptRoot/agentlist.psm1

if (-not $Agents) {
    $Agents = Get-Agents | ? { ($_.OS -eq 'Windows') -and ($_.Category -ne 'Codesign') } | % { $_.Name }
}

$PWord = ConvertTo-SecureString –String $teamAgentServiceAccountPassword –AsPlainText -Force
$creds = New-Object –TypeName System.Management.Automation.PSCredential –ArgumentList $teamAgentServiceAccountName, $PWord

foreach ($agent in $Agents)
{
	Write-Host "`nChanging password for agent '$agent'..."

	$psSession = New-PSSession -ComputerName $agent -credential $creds

	$passwordChangeScript="$PSScriptRoot\ChangePassword.ps1"

    try
    {
        Invoke-Command -Session $psSession -FilePath $passwordChangeScript -ArgumentList $teamAgentServiceAccountName, $teamAgentServiceAccountPassword
    }
    catch
    {
        Write-Error $_.Exception.Message
    }
    finally
    {
        Remove-PSSession $psSession
    }
}
