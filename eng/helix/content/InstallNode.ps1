
param(
    [Parameter(Mandatory = $true)]
    $Version,
    
    [Parameter(Mandatory = $true)]
    $output_dir
)

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue' # Workaround PowerShell/PowerShell#2138

Set-StrictMode -Version 1

if ((Get-Command "node.exe" -ErrorAction SilentlyContinue)) 
{ 
    Write-Host "Found node.exe in PATH"
    exit
}

if (Test-Path "$output_dir\node.exe")
{
    Write-Host "Node.exe found at $output_dir"
    exit
}

$nodeFile="node-v$Version-win-x64"
$url="http://nodejs.org/dist/v$Version/$nodeFile.zip"
Write-Host "Starting download of NodeJs ${Version} from $url"
Invoke-WebRequest -UseBasicParsing -Uri "$url" -OutFile "nodejs.zip"
Write-Host "Done downloading NodeJS ${Version}"

$temp_dir = Join-Path ([System.IO.Path]::GetTempPath()) [System.Guid]::NewGuid()
mkdir $temp_dir -Force
Write-Host "Extracting to $temp_dir"

if (Get-Command -Name 'Microsoft.PowerShell.Archive\Expand-Archive' -ErrorAction Ignore) {
    # Use built-in commands where possible as they are cross-plat compatible
    Microsoft.PowerShell.Archive\Expand-Archive -Path "nodejs.zip" -DestinationPath $temp_dir
}
else {
    # Fallback to old approach for old installations of PowerShell
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    [System.IO.Compression.ZipFile]::ExtractToDirectory("nodejs.zip", $temp_dir)
}

Write-Host "Expanded NodeJs"
mkdir $output_dir -Force
copy nodejs/$nodeFile/node.exe $output_dir
