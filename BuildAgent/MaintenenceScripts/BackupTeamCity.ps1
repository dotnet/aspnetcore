[CmdletBinding()]
param(
    [string]$server,
    [string]$username,
    [string]$password
)
function Invoke-HTTPPostCommand() {
    param(
        [string] $url,
        [string] $username,
        [string] $password
    )
 
    $authInfo = $username + ":" + $password
    $authInfo = [System.Convert]::ToBase64String([System.Text.Encoding]::Default.GetBytes($authInfo))
 
    $webRequest = [System.Net.WebRequest]::Create($url)
    $webRequest.ContentType = "text/html"
    $PostStr = [System.Text.Encoding]::Default.GetBytes("")
    $webrequest.ContentLength = $PostStr.Length
    $webRequest.Headers["Authorization"] = "Basic " + $authInfo
    $webRequest.PreAuthenticate = $true
    $webRequest.Method = "POST"
 
    $requestStream = $webRequest.GetRequestStream()
    $requestStream.Write($PostStr, 0, $PostStr.length)
    $requestStream.Close()
 
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
    $TeamCityURL = [System.String]::Format("{0}/httpAuth/app/rest/server/backup?addTimestamp={1}&includeConfigs={2}&includeDatabase={3}&includeBuildLogs={4}&includePersonalChanges={5}&fileName={6}",
                                            $server,
                                            $addTimestamp,
                                            $includeConfigs,
                                            $includeDatabase,
                                            $includeBuildLogs,
                                            $includePersonalChanges,
                                            $fileName);
 
    Invoke-HTTPPostCommand $TeamCityURL $username $password
}

$addTimestamp = $true
$includeConfigs = $true
$includeDatabase = $true
$includeBuildLogs = $true
$includePersonalChanges = $true
$fileName = "TeamCity_Backup_"
 
Invoke-TeamCityBackup $server $addTimestamp $includeConfigs $includeDatabase $includeBuildLogs $includePersonalChanges $fileName $username $password