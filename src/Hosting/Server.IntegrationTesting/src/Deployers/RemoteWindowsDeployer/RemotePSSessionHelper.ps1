[Diagnostics.CodeAnalysis.SuppressMessageAttribute(
 'PSAvoidUsingConvertToSecureStringWithPlainText',
 '',
 Justification='$accountPassword is a script parameter so it needs to be plain-text')]
[CmdletBinding()]
param(
	[Parameter(Mandatory=$true)]
	[string]$serverName,

	[Parameter(Mandatory=$true)]
	[string]$accountName,

	[Parameter(Mandatory=$true)]
	[string]$accountPassword,

	[Parameter(Mandatory=$true)]
	[string]$deployedFolderPath,

	[Parameter(Mandatory=$false)]
	[string]$dotnetRuntimePath = "",

	[Parameter(Mandatory=$true)]
	[string]$executablePath,

	[Parameter(Mandatory=$false)]
	[string]$executableParameters,

	[Parameter(Mandatory=$true)]
	[string]$serverType,

	[Parameter(Mandatory=$true)]
	[string]$serverAction,

	[Parameter(Mandatory=$true)]
	[string]$applicationBaseUrl,

	[Parameter(Mandatory=$false)]
	[string]$environmentVariables
)

Write-Host "`nExecuting deployment helper script on machine '$serverName'"
Write-Host "`nStarting a powershell session to machine '$serverName'"

$securePassword = ConvertTo-SecureString $accountPassword -AsPlainText -Force
$credentials= New-Object System.Management.Automation.PSCredential ($accountName, $securePassword)
$psSession = New-PSSession -ComputerName $serverName -credential $credentials

$remoteResult="0"
if ($serverAction -eq "StartServer")
{
	Write-Host "Starting the application on machine '$serverName'"
	$startServerScriptPath = "$PSScriptRoot\StartServer.ps1"
	$remoteResult=Invoke-Command -Session $psSession -FilePath $startServerScriptPath -ArgumentList $deployedFolderPath, $dotnetRuntimePath, $executablePath, $executableParameters, $serverType, $serverName, $applicationBaseUrl, $environmentVariables
}
else
{
	Write-Host "Stopping the application on machine '$serverName'"
	$stopServerScriptPath = "$PSScriptRoot\StopServer.ps1"
	$serverProcessName = [System.IO.Path]::GetFileNameWithoutExtension($executablePath)
	$remoteResult=Invoke-Command -Session $psSession -FilePath $stopServerScriptPath -ArgumentList $deployedFolderPath, $serverProcessName, $serverType, $serverName
}

Remove-PSSession $psSession

# NOTE: Currenty there is no straight forward way to get the exit code from a remotely executing session, so
# we print out the exit code in the remote script and capture it's output to get the exit code.
if($remoteResult.Length > 0)
{
    $finalExitCode=$remoteResult[$remoteResult.Length-1]
    exit $finalExitCode
}
