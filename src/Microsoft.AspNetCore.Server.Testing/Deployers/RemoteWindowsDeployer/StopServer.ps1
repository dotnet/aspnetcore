[CmdletBinding()]
param(
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
	throw [System.NotImplementedException] "IIS deployment scenarios not yet implemented."
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
		Stop-Process -Id $serverProcess.Id
	}
}

# NOTE: Make sure this is the last statement in this script as its used to get the exit code of this script
$LASTEXITCODE