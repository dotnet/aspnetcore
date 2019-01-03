[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$ciServer,
    [Parameter(Mandatory = $true)][string]$ciUsername,
    [Parameter(Mandatory = $true)][string]$ciPassword,
    [Parameter(Mandatory = $true)][string]$azureAccount,
    [Parameter(Mandatory = $true)][string]$azureKey,
    [Parameter(Mandatory = $true)][string]$azureShare
)

Set-StrictMode -Version 2
$ErrorActionPreference = 'Stop'

function Save-TeamCityBackup() {
    param(
        [string] $server,
        [string] $fileName,
        [string] $userName,
        [string] $password,
        [string] $targetFolder
    )
    $TeamCityURL = [System.String]::Format("http://{0}/get/file/backup/{1}", $server, $fileName);

    $targetFile = $targetFolder + $fileName.Replace('/', '_' )

    $authInfo = $username + ":" + $password
    $authInfo = [System.Convert]::ToBase64String([System.Text.Encoding]::Default.GetBytes($authInfo))

    Write-Host "Url: $TeamCityURL
    OutFile: $targetFile"

    $webClient = new-object System.Net.WebClient
    $webClient.Headers["Authorization"] = "Basic $authInfo"

    $retries = 3
    $secondsDelay = 60
    $retrycount = 0
    $completed = $false

    while (-not $completed) {
        try {
            $webClient.DownloadFile($TeamCityURL, $targetFolder + $fileName)
            $completed = $true
        }
        catch {
            if ($retrycount -ge $retries) {
                Write-Host "Backup download failed the max of $retries times"
                Write-Error $_.Exception.Message
                throw
            }
            else {
                Write-Host "Command failed. Retrying in $secondsDelay seconds."
                Start-Sleep $secondsDelay
                $retrycount++ 
            }
        }
    }
}

function Invoke-TeamCityRequest() {
    param(
        [string] $server,
        [string] $url,
        [string] $username,
        [string] $password,
        [string] $verb
    )
    $TeamCityURL = [System.String]::Format("http://{0}/httpAuth/app/rest/{1}", $server, $url);
    
    $authInfo = $username + ":" + $password
    $authInfo = [System.Convert]::ToBase64String([System.Text.Encoding]::Default.GetBytes($authInfo))

    $webRequest = [System.Net.WebRequest]::Create($TeamCityURL)
    $webRequest.ContentType = "text/html"
    $webRequest.Timeout = 60000
    $PostStr = [System.Text.Encoding]::Default.GetBytes("")
    $webrequest.ContentLength = $PostStr.Length
    $webRequest.Headers["Authorization"] = "Basic " + $authInfo
    $webRequest.PreAuthenticate = $true
    $webRequest.Method = $verb
 
    if ($verb -eq "POST") {
        $requestStream = $webRequest.GetRequestStream()
        $requestStream.Write($PostStr, 0, $PostStr.length)
        $requestStream.Close()
    }
    Write-Host $TeamCityURL
    [System.Net.WebResponse] $resp = $webRequest.GetResponse();
    $rs = $resp.GetResponseStream();
    [System.IO.StreamReader] $sr = New-Object System.IO.StreamReader -argumentList $rs;
    [string] $results = $sr.ReadToEnd();

    return $results;
}

function Invoke-TeamCityBackup() {
    param(
        [string] $server,
        [string] $addTimestamp,
        [string] $includeConfigs,
        [string] $includeDatabase,
        [string] $includeBuildLogs,
        [string] $includePersonalChanges,
        [string] $fileName,
        [string] $userName,
        [string] $password
    )
    $api = [System.String]::Format("server/backup?addTimestamp={0}&includeConfigs={1}&includeDatabase={2}&includeBuildLogs={3}&includePersonalChanges={4}&fileName={5}",
        $addTimestamp,
        $includeConfigs,
        $includeDatabase,
        $includeBuildLogs,
        $includePersonalChanges,
        $fileName);

    return Invoke-TeamCityRequest $server $api $userName $password "POST"
}

function Get-BackupStatus {
    param(
        [Parameter(Mandatory = $true)][string]$server,
        [Parameter(Mandatory = $true)][string]$username,
        [Parameter(Mandatory = $true)][string]$password
    )
    $api = "server/backup"
    return Invoke-TeamCityRequest $server $api $username $password "GET"
}

function Start-Backup {
    param(
        [Parameter(Mandatory = $true)][string]$server,
        [Parameter(Mandatory = $true)][string]$username,
        [Parameter(Mandatory = $true)][string]$password
    )
    $addTimestamp = $true
    $includeConfigs = $true
    $includeDatabase = $true
    $includeBuildLogs = $false
    $includePersonalChanges = $true
    $fileName = "TeamCity_Backup_"
    
    return Invoke-TeamCityBackup $server $addTimestamp $includeConfigs $includeDatabase $includeBuildLogs $includePersonalChanges $fileName $username $password
}

$backupName = Start-Backup $ciServer $ciUsername $ciPassword

# Wait for the backup to finish
while ((Get-BackupStatus $ciServer $ciUsername $ciPassword) -eq "Running") {
    Start-Sleep -Seconds 60
}

Save-TeamCityBackup $ciServer $backupName $ciUsername $ciPassword "$PSScriptRoot/"

$backupName = $backupName.Replace('/', '_')

$localFile = "$PSScriptRoot/$backupName"

$arguments = "$azureAccount $azureKey $azureShare $localFile $backupName"
Invoke-Expression "$PSScriptRoot/../../azure/upload-to-file-storage.ps1 $arguments"
