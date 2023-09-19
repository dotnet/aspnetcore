# Globals... for now.
$Configuration = "Release"

function Build-Project
{
    param (
        [string]$ProjectName
    )

    $ProjectFiles = @(Get-ChildItem -Path . -Filter $ProjectName -Recurse -File)
    if ($ProjectFiles.Length -eq 0) {
        throw "Couldn't find project $ProjectName."
    }
    if ($ProjectFiles.Length -gt 1) {
        throw "Too many results for project $ProjectName."
    }

    $ProjectFullPath = $ProjectFiles[0].FullName

    Write-Output "Building $ProjectFullPath"

    dotnet pack -c $Configuration $ProjectFullPath
    if (!$?) {
        throw "Failed to build project $ProjectFullPath."
    }
}

function Build-AspNetCore {
    Push-Location -Path src/Servers/Kestrel
    dotnet pack Kestrel.slnf
    Pop-Location

    foreach ($line in Get-Content .\pack-list.txt) {
        if ([string]::IsNullOrWhiteSpace($line)) {
            continue
        }

        Push-Location
        Build-Project $line
        Pop-Location
    }
}

Build-AspNetCore
