 <# 
 .SYNOPSIS 
     Installs dotnet-install.ps1 from https://dot.net/v1/dotnet-install.ps1
 .DESCRIPTION 
     Installs dotnet-install.ps1 from https://dot.net/v1/dotnet-install.ps1
 #> 

$installScript = 'dotnet-install.ps1'
if (!(Test-Path $installScript)) {
  $ProgressPreference = 'SilentlyContinue' # Don't display the console progress UI - it's a huge perf hit

  $maxRetries = 5
  $retries = 1

  $uri = "https://dot.net/v1/dotnet-install.ps1"

  while($true) {
    try {
      Write-Host "GET $uri"
      Invoke-WebRequest $uri -OutFile $installScript
      break
    }
    catch {
      Write-Host "Failed to download '$uri'"
      Write-Error $_.Exception.Message -ErrorAction Continue
    }

    if (++$retries -le $maxRetries) {
      $delayInSeconds = [math]::Pow(2, $retries) - 1 # Exponential backoff
      Write-Host "Retrying. Waiting for $delayInSeconds seconds before next attempt ($retries of $maxRetries)."
      Start-Sleep -Seconds $delayInSeconds
    }
    else {
      throw "Unable to download file in $maxRetries attempts."
    }
  }
}
