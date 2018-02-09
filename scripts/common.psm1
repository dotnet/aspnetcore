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
        [string]$RepoRoot,
        [switch]$Shipping
    )

    $moduleConfigFile = Join-Path $RepoRoot ".gitmodules"
    $submodules = @()

    [xml] $submoduleConfig = Get-Content "$RepoRoot/build/submodules.props"
    $repos = $submoduleConfig.Project.ItemGroup.Repository | % { $_.Include }

    Get-ChildItem "$RepoRoot/modules/*" -Directory `
    | ? { (-not $Shipping) -or $($repos -contains $($_.Name)) -or $_.Name -eq 'Templating' } `
    | % {
        Push-Location $_ | Out-Null
        Write-Verbose "Attempting to get submodule info for $_"

        if (Test-Path 'version.props') {
            [xml] $versionXml = Get-Content 'version.props'
            $versionPrefix = $versionXml.Project.PropertyGroup.VersionPrefix
        } else {
            $versionPrefix = ''
        }

        try {
            $data = [PSCustomObject] @{
                path      = $_
                module    = $_.Name
                commit    = $(git rev-parse HEAD)
                newCommit = $null
                changed   = $false
                branch    = $(git config -f $moduleConfigFile --get submodule.modules/$($_.Name).branch )
                versionPrefix = $versionPrefix
            }

            $submodules += $data
        }
        finally {
            Pop-Location | Out-Null
        }
    }

    return $submodules
}

function SaveXml([xml]$xml, [string]$path) {
    Write-Verbose "Saving to $path"
    $ErrorActionPreference = 'stop'

    $settings = New-Object System.XML.XmlWriterSettings
    $settings.OmitXmlDeclaration = $true
    $settings.Encoding = New-Object System.Text.UTF8Encoding( $true )
    $writer = [System.XML.XMLTextWriter]::Create($path, $settings)
    $xml.Save($writer)
    $writer.Close()
}

function LoadXml([string]$path) {
    Write-Verbose "Reading from $path"

    $ErrorActionPreference = 'stop'
    $obj = new-object xml
    $obj.PreserveWhitespace = $true
    $obj.Load($path)
    return $obj
}

function PackageIdVarName([string]$packageId) {
    $canonicalVarName = ''
    $upperCaseNext = $true
    for ($i = 0; $i -lt $packageId.Length; $i++) {
        $ch = $packageId[$i]
        if (-not [System.Char]::IsLetterOrDigit(($ch))) {
            $upperCaseNext = $true
            continue
        }
        if ($upperCaseNext) {
            $ch = [System.Char]::ToUpperInvariant($ch)
            $upperCaseNext = $false
        }
        $canonicalVarName += $ch
    }
    $canonicalVarName += "PackageVersion"
    return $canonicalVarName
}
