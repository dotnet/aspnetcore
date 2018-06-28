$customHive = "$PSScriptRoot/CustomHive"

function Create-CustomHive
{
    Param()
    Remove-Item -Path $customHive -Recurse -ErrorAction Ignore
    New-Item -ErrorAction Ignore -Path $customHive -ItemType Directory

    Clean-CustomHive
}

function Clean-CustomHive
{
    Param()
    $uninstallResults = Call-DotnetNew "--uninstall"
    Write-Host "uninstallresults: $uninstallResults"
    $currently = "Currently installed items:"
    $templatePackString = ($uninstallResults -split $currently)
    Write-Host "tempPack: $templatePackString"
    $templatePackages = $templatePackString.Split([Environment]::NewLine)

    foreach($package in $templatePackages)
    {
        Write-Host $package
        #Call-DotnetNew "--uninstall $package"
    }
}
function Call-DotnetNew($arguments) {
    & "dotnet" new $arguments --debug:custom-hive $customHive
}

Create-CustomHive