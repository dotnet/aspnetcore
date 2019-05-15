
param(
    [Parameter(Mandatory = $true)]
    $Version
)

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue' # Workaround PowerShell/PowerShell#2138

Set-StrictMode -Version 1

if (Test-Path "%HELIX_CORRELATION_PAYLOAD%\node\bin\node.exe")
{
    Write-Host "Node.exe found at %HELIX_CORRELATION_PAYLOAD%\node\bin"
    exit
}

$nodeFile="node-v$Version-win-x64"
$url="http://nodejs.org/dist/v$Version/$nodeFile.zip"
Write-Host "Starting download of NodeJs ${Version} from $url"
Invoke-WebRequest -UseBasicParsing -Uri "$url" -Out "nodejs.zip"
Write-Host "Done downloading NodeJS ${Version}"
mkdir node -Force
Expand-Archive "nodejs.zip" -destination node
Write-Host "Expanded NodeJs"
mkdir %HELIX_CORRELATION_PAYLOAD%\node\bin -Force
copy node/$nodeFile/node.exe %HELIX_CORRELATION_PAYLOAD%\node\bin
