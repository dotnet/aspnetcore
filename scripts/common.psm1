function Assert-Git {
    if (!(Get-Command git -ErrorAction Ignore)) {
        Write-Error 'git is required to execute this script'
        exit 1
    }
}

function Invoke-Block([scriptblock]$cmd) {
    & $cmd

    # Need to check both of these cases for errors as they represent different items
    # - $?: did the powershell script block throw an error
    # - $lastexitcode: did a windows command executed by the script block end in error
    if ((-not $?) -or ($lastexitcode -ne 0)) {
        throw "Command failed to execute: $cmd"
    }
}
