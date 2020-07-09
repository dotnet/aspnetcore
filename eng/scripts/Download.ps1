 <# 
 .SYNOPSIS 
     Downloads a given uri and saves it to outputFile
 .DESCRIPTION
     Downloads a given uri and saves it to outputFile
 PARAMETER uri
    The uri to fetch
.PARAMETER outputFile
    The outputh file path to save the uri
#>
param(
    [Parameter(Mandatory = $true)]
    $uri,

    [Parameter(Mandatory = $true)]
    $outputFile
)
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
$ProgressPreference = 'SilentlyContinue' # Don't display the console progress UI - it's a huge perf hit

$maxRetries = 5
$retries = 1

while($true) {
    try {
      Write-Host "GET $uri"
      Invoke-WebRequest $uri -OutFile $outputFile
      break
    }
    catch {
      Write-Host "Failed to download '$uri'"
      $error = $_.Exception.Message
    }

    if (++$retries -le $maxRetries) {
      Write-Warning $error -ErrorAction Continue
      $delayInSeconds = [math]::Pow(2, $retries) - 1 # Exponential backoff
      Write-Host "Retrying. Waiting for $delayInSeconds seconds before next attempt ($retries of $maxRetries)."
      Start-Sleep -Seconds $delayInSeconds
    }
    else {
      Write-Error $error -ErrorAction Continue
      throw "Unable to download file in $maxRetries attempts."
    }
 }

Write-Host "Download of '$uri' complete, saved to $outputFile..."
