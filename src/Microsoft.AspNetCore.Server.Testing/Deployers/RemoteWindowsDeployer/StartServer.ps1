[CmdletBinding()]
param(
	[Parameter(Mandatory=$true)]
	[string]$executablePath,

	[Parameter(Mandatory=$false)]
	[string]$executableParameters,

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

# Temporary workaround for issue https://github.com/dotnet/cli/issues/2967
if ($executablePath -ne "dotnet.exe"){
    $destinationDir = Split-Path $executablePath
    Copy-Item C:\Windows\System32\forwarders\shell32.dll $destinationDir
}

$command = $executablePath + " " + $executableParameters + " --server.urls " + $applicationBaseUrl
if ($serverType -eq "IIS")
{
	throw [System.NotImplementedException] "IIS deployment scenarios not yet implemented."
}
elseif ($serverType -eq "Kestrel")
{
	$command = $command + " --server Microsoft.AspNetCore.Server.Kestrel"
	Write-Host "Executing the command '$command'"
	Invoke-Expression $command
}
elseif ($serverType -eq "WebListener")
{
	$command = $command + " --server Microsoft.AspNetCore.Server.WebListener"
	Write-Host "Executing the command '$command'"
	Invoke-Expression $command
}
else
{
	throw [System.InvalidOperationException] "Server type '$serverType' is not supported."
}

# NOTE: Make sure this is the last statement in this script as its used to get the exit code of this script
$LASTEXITCODE