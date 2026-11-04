[CmdletBinding()]
param(
	[Parameter(Mandatory=$true)]
	[string]$deployedFolderPath,

	[Parameter(Mandatory=$true)]
	[string]$serverProcessName,

	[Parameter(Mandatory=$true)]
	[string]$serverType,

	[Parameter(Mandatory=$true)]
	[string]$serverName
)

function DoesCommandExist($command)
{
	$oldPreference = $ErrorActionPreference
	$ErrorActionPreference="stop"

	try
	{
		if (Get-Command $command)
		{
			return $true
		}
	}
	catch
	{
		Write-Host "Command '$command' does not exist"
		return $false
	}
	finally
	{
		$ErrorActionPreference=$oldPreference
	}
}

Write-Host "Executing the stop server script on machine '$serverName'"

if ($serverType -eq "IIS")
{
	$publishedDirName=Split-Path $deployedFolderPath -Leaf
	Write-Host "Stopping the IIS website '$publishedDirName'"
	Import-Module IISAdministration
	Stop-IISSite -Name $publishedDirName -Confirm:$false
	Remove-IISSite -Name $publishedDirName -Confirm:$false
    net stop w3svc
    net start w3svc
}
else
{
    Write-Host "Stopping the process '$serverProcessName'"
    $serverProcess=Get-Process -Name "$serverProcessName"

    if (DoesCommandExist("taskkill"))
    {
	    # Kill the parent and child processes
	    & taskkill /pid $serverProcess.Id /t /f
    }
    else
    {
	    Stop-Process -Id $serverProcess.Id -Force
    }
}

# NOTE: Make sure this is the last statement in this script as its used to get the exit code of this script
$LASTEXITCODE