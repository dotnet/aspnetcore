[CmdletBinding()]
param(
	[Parameter(Mandatory=$true)]
	[string]$executablePath,
	
	[Parameter(Mandatory=$true)]
	[string]$serverType,

	[Parameter(Mandatory=$true)]
	[string]$serverName,

	[Parameter(Mandatory=$true)]
	[string]$applicationBaseUrl,

	# These are of the format: key1=value1,key2=value2,key3=value3
	[Parameter(Mandatory=$false)]
	[string]$environmentVariables
)

Write-Host "Executing the start server script on machine '$serverName'"

IF (-Not [string]::IsNullOrWhitespace($environmentVariables))
{
	Write-Host "Setting up environment variables"
	foreach ($envVariablePair in $environmentVariables.Split(",")){
		$pair=$envVariablePair.Split("=");
		[Environment]::SetEnvironmentVariable($pair[0], $pair[1])
	}
}

if ($serverType -eq "IIS")
{
	throw [System.NotImplementedException] "IIS deployment scenarios not yet implemented."
}
elseif ($serverType -eq "Kestrel")
{
	Write-Host "Starting the process '$executablePath'"
	& $executablePath --server.urls $applicationBaseUrl
}
elseif ($serverType -eq "WebListener")
{
	Write-Host "Starting the process '$executablePath'"
	& $executablePath --server.urls $applicationBaseUrl --server "Microsoft.AspNetCore.Server.WebListener"
}
else
{
	throw [System.InvalidOperationException] "Server type '$serverType' is not supported."
}

# NOTE: Make sure this is the last statement in this script as its used to get the exit code of this script
$LASTEXITCODE