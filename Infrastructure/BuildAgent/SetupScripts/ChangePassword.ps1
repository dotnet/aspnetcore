[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)]
    [string]$teamAgentServiceAccountName,

    [Parameter(Mandatory=$true)]
    [string]$teamAgentServiceAccountPassword
)

$Service = Get-WmiObject -Class Win32_Service -Filter  "name like '%TCBuildAgent%'"

$result=$Service.Change($Null,$Null,$Null,$Null,$Null,$false,$teamAgentServiceAccountName,$teamAgentServiceAccountPassword,$Null,$Null,$Null).ReturnValue

if ($result -eq '0')
{
    Write-Host "Stopping the service..."
    $result=$Service.StopService().ReturnValue
    
    if ($result -ne '0')
    {
        Write-Error "Failed to stop the service. Result code: $result"
        Write-Host "Trying to start the service anyway as earlier error could be because the service was already in stopped state"
    }
    
    # Try to sleep only if stopping the service call succeeded
    if ($result -eq '0')
    {
        $sleepTimeInSeconds=10
        Write-Host "Sleeping for '$sleepTimeInSeconds' seconds to enable the service to stop completely"
        Start-Sleep -Seconds $sleepTimeInSeconds
    }

    Write-Host "Starting the service..."
    $result=$Service.StartService().ReturnValue

    if ($result -eq '0')
    {
        Write-Host "Service started successfully." -ForegroundColor Green
    }
    else
    {
        Write-Error "Failed to start the service. Result code: $result"
    }
}
else
{
    Write-Error "Password change failed. Result code: $result"
}
