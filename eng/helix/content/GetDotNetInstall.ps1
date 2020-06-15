 <# 
 .SYNOPSIS 
     Installs dotnet sdk and runtime using https://dot.net/v1/dotnet-install.ps1
 .DESCRIPTION
     Installs dotnet sdk and runtime using https://dot.net/v1/dotnet-install.ps1
.PARAMETER arch
    The architecture to install.
.PARAMETER sdkVersion
    The sdk version to install
.PARAMETER runtimeVersion
    The runtime version to install
.PARAMETER installDir
    The directory to install to
#>
param(
    [Parameter(Mandatory = $true)]
    $arch,

    [Parameter(Mandatory = $true)]
    $sdkVersion,
    
    [Parameter(Mandatory = $true)]
    $runtimeVersion,
    
    [Parameter(Mandatory = $true)]
    $installDir    
)
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
$ProgressPreference = 'SilentlyContinue' # Don't display the console progress UI - it's a huge perf hit

$maxRetries = 5
$retries = 1

$uri = "https://dot.net/v1/dotnet-install.ps1"

while($true) {
    try {
      Write-Host "GET $uri"
      $installScript = Invoke-WebRequest $uri
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

Write-Host "Download of '$uri' complete..."
$dotnetInstall = ([scriptblock]::Create($installScript));
Write-Host "Installing SDK..."
&$dotnetInstall -Architecture $arch -Version $sdkVersion -InstallDir $installDir
Write-Host "Installing Runtime..."
&$dotnetInstall -Architecture $arch -Runtime dotnet -Version $runtimeVersion -InstallDir $installDir
 
