[CmdletBinding()]
param(
    [string]$teamAgentServiceAccountName="redmond\asplab",

    [Parameter(Mandatory=$true)]
    [string]$teamAgentServiceAccountPassword
)

$agents = @()
$allLines = Get-Content("$PSScriptRoot\agentlist.txt")
foreach ($line in $allLines) 
{
    $line=$line.Trim()

    if ( -Not ([string]::IsNullOrEmpty($line)))
    {
        # Ignore lines which could be comments
        if ( -Not $line.StartsWith("#"))
        {
            $agents=$agents + $line
        }
    }
}

$PWord = ConvertTo-SecureString –String $teamAgentServiceAccountPassword –AsPlainText -Force
$creds = New-Object –TypeName System.Management.Automation.PSCredential –ArgumentList $teamAgentServiceAccountName, $PWord

foreach ($agent in $agents)
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