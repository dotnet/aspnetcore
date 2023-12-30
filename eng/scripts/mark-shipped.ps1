[CmdletBinding(PositionalBinding=$false)]
param ()

Set-StrictMode -version 2.0
$ErrorActionPreference = "Stop"

function MarkShipped([string]$dir) {
    $shippedFilePath = Join-Path $dir "PublicAPI.Shipped.txt"
    $shipped = @()
    $shipped += Get-Content $shippedFilePath

    $unshippedFilePath = Join-Path $dir "PublicAPI.Unshipped.txt"
    $unshipped = Get-Content $unshippedFilePath
    $removed = @()
    $removedPrefix = "*REMOVED*";
    Write-Host "Processing $dir"

    foreach ($item in $unshipped) {
        if ($item.Length -gt 0) {
            if ($item.StartsWith($removedPrefix)) {
                $item = $item.Substring($removedPrefix.Length)
                $removed += $item
            }
            else {
                $shipped += $item
            }
        }
    }

    $shipped | Sort-Object -Unique |Where-Object { -not $removed.Contains($_) } | Out-File $shippedFilePath -Encoding Ascii
    Copy-Item eng/PublicAPI.empty.txt $unshippedFilePath
}

try {
    foreach ($file in Get-ChildItem -re -in "PublicApi.Shipped.txt") {
        $dir = Split-Path -parent $file
        MarkShipped $dir
    }
}
catch {
    Write-Host $_
    Write-Host $_.Exception
    exit 1
}
