# Takes an input browser log file and splits it into separate files for each browser
param(
    [Parameter(Mandatory=$true, Position=0)][string]$InputFile,
    [Parameter(Mandatory=$false)][string]$OutputDirectory,
)

if(!$OutputDirectory) {
    $OutputDirectory = Split-Path -Parent $InputFile
}