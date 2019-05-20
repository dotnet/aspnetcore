
param(
    [Parameter(Mandatory = $true)]
    $Version
)

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue' # Workaround PowerShell/PowerShell#2138

Set-StrictMode -Version 1

if ((Get-Command "node.exe" -ErrorAction SilentlyContinue)) 
{ 
    Write-Host "Found node.exe in PATH"
    exit
}

if (Test-Path "%HELIX_CORRELATION_PAYLOAD%\node\bin\node.exe")
{
    Write-Host "Node.exe found at %HELIX_CORRELATION_PAYLOAD%\node\bin"
    exit
}

$nodeFile="node-v$Version-win-x64"
$url="http://nodejs.org/dist/v$Version/$nodeFile.zip"
Write-Host "Starting download of NodeJs ${Version} from $url"
Invoke-WebRequest -UseBasicParsing -Uri "$url" -OutFile "nodejs.zip"
Write-Host "Done downloading NodeJS ${Version}"

if ((Get-Command "Expand-Archive" -ErrorAction SilentlyContinue)) 
{ 
    Expand-Archive "nodejs.zip"
}
else
{
    $shell = new-object -com shell.application
    $zip = $shell.NameSpace("nodejs.zip")
    foreach($item in $zip.items())
    {
        $shell.Namespace($output_dir).copyhere($item)
    }
    Start-Sleep -s 5
}

Write-Host "Expanded NodeJs"
mkdir %HELIX_CORRELATION_PAYLOAD%\node\bin -Force
copy nodejs/$nodeFile/node.exe %HELIX_CORRELATION_PAYLOAD%\node\bin
