
param(
    [Parameter(Mandatory = $true)]
    $Version,
    
    [Parameter(Mandatory = $true)]
    $output_dir
)

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue' # Workaround PowerShell/PowerShell#2138

Set-StrictMode -Version 1

if (Get-Command "node.exe" -ErrorAction SilentlyContinue)
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

$temppath = [System.IO.Path]::GetTempPath()
$temp_dir = Join-Path $temppath nodejs
New-Item -Path "$temp_dir" -ItemType "directory" -Force
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
New-Item -Path "$output_dir" -ItemType "directory" -Force
Write-Host "Copying $temp_dir\$nodeFile\node.exe to $output_dir"
Copy-Item "$temp_dir\$nodeFile\node.exe" "$output_dir\node.exe"

if (Test-Path "$output_dir\node.exe")
{
    Write-Host "Node.exe copied to $output_dir"
}
else
{
    Write-Host "Node.exe not found at $output_dir"
}
