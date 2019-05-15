
param(
    [Parameter(Mandatory = $true)]
    $Version
)

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue' # Workaround PowerShell/PowerShell#2138

Set-StrictMode -Version 1

$nodeFile="node-v$Version-win-x64"
$url="http://nodejs.org/dist/v$Version/$nodeFile.zip"
Write-Host "Starting download of NodeJs ${Version} from $url"
Invoke-WebRequest -UseBasicParsing -Uri "$url" -Out "nodejs.zip"
Write-Host "Done downloading NodeJS ${Version}"
mkdir node -Force
Expand-Archive "nodejs.zip" -destination node
Write-Host "Expanded NodeJs"
copy node/$nodeFile/node.exe .
