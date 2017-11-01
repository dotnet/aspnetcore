function Assert-Git {
    if (!(Get-Command git -ErrorAction Ignore)) {
        Write-Error 'git is required to execute this script'
        exit 1
    }
}

function Invoke-Block([scriptblock]$cmd) {
    $cmd | Out-String | Write-Verbose
    & $cmd

    # Need to check both of these cases for errors as they represent different items
    # - $?: did the powershell script block throw an error
    # - $lastexitcode: did a windows command executed by the script block end in error
    if ((-not $?) -or ($lastexitcode -ne 0)) {
        Write-Warning $error[0]
        throw "Command failed to execute: $cmd"
    }
}

function Get-Submodules {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RepoRoot
    )

    Invoke-Block { & git submodule update --init } | Out-Null

    $moduleConfigFile = Join-Path $RepoRoot ".gitmodules"
    $submodules = @()

    Get-ChildItem "$RepoRoot/modules/*" -Directory | % {
        Push-Location $_ | Out-Null
        Write-Verbose "Attempting to get submodule info for $_"
        try {
            $data = @{
                path      = $_
                module    = $_.Name
                commit    = $(git rev-parse HEAD)
                newCommit = $null
                changed   = $false
                branch    = $(git config -f $moduleConfigFile --get submodule.modules/$($_.Name).branch )
            }

            $submodules += $data
        }
        finally {
            Pop-Location | Out-Null
        }
    }

    return $submodules
}
