.\..\..\build.cmd -pack -all

Add-Type -AssemblyName System.IO.Compression.FileSystem
function Unzip {
    param([string]$zipfile, [string]$outpath)

    [System.IO.Compression.ZipFile]::ExtractToDirectory($zipfile, $outpath)
}
function New-TemporaryDirectory {
    $parent = [System.IO.Path]::GetTempPath()
    [string] $name = [System.Guid]::NewGuid()
    New-Item -ItemType Directory -Path (Join-Path $parent $name)
}
Set-StrictMode -Version 2
$ErrorActionPreference = 'Stop'


$dotnetExePath = (Get-Command dotnet).Source
$dotnetHome = Split-Path -Path $dotnetExePath -Parent
$tempDir = New-TemporaryDirectory
Write-Host "$tempDir"
Unzip "$PSScriptRoot\..\..\artifacts\packages\Debug\Shipping\AspNetCoreRuntime.3.0.x64.3.0.0-preview-t000.nupkg" "$tempDir"
Copy-Item "$tempDir\content\*" -Destination $dotnetHome -Recurse -ErrorAction SilentlyContinue

dotnet test test/
