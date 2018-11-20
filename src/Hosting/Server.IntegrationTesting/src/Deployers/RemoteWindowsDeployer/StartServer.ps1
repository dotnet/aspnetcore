[CmdletBinding()]
param(
	[Parameter(Mandatory=$true)]
	[string]$deployedFolderPath,

	[Parameter(Mandatory=$false)]
	[string]$dotnetRuntimePath,

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

if ($serverType -eq "IIS")
{
    $publishedDirName=Split-Path $deployedFolderPath -Leaf
    Write-Host "Creating IIS website '$publishedDirName' for path '$deployedFolderPath'"
    Import-Module IISAdministration
    $port=([System.Uri]$applicationBaseUrl).Port
    $bindingPort="*:" + $port + ":"
    New-IISSite -Name $publishedDirName -BindingInformation $bindingPort -PhysicalPath $deployedFolderPath
}
elseif (($serverType -eq "Kestrel") -or ($serverType -eq "WebListener"))
{
    if (-Not [string]::IsNullOrWhitespace($environmentVariables))
    {
        Write-Host "Setting up environment variables"
        foreach ($envVariablePair in $environmentVariables.Split(","))
        {
	        $pair=$envVariablePair.Split("=");
	        [Environment]::SetEnvironmentVariable($pair[0], $pair[1])
        }
    }

    if ($executablePath -eq "dotnet.exe")
    {
        Write-Host "Setting the dotnet runtime path to the PATH environment variable"
        [Environment]::SetEnvironmentVariable("PATH", "$dotnetRuntimePath")
    }

    # Change the current working directory to the deployed folder to make applications work 
    # when they use API like Directory.GetCurrentDirectory()
    cd -Path $deployedFolderPath

    $command = $executablePath + " " + $executableParameters + " --server.urls " + $applicationBaseUrl
    if ($serverType -eq "Kestrel")
    {
	    $command = $command + " --server Microsoft.AspNetCore.Server.Kestrel"
	    Write-Host "Executing the command '$command'"
	    Invoke-Expression $command
    }
    elseif ($serverType -eq "WebListener")
    {
	    $command = $command + " --server Microsoft.AspNetCore.Server.HttpSys"
	    Write-Host "Executing the command '$command'"
	    Invoke-Expression $command
    }
}
else
{
    throw [System.InvalidOperationException] "Server type '$serverType' is not supported."
}

# NOTE: Make sure this is the last statement in this script as its used to get the exit code of this script
$LASTEXITCODE