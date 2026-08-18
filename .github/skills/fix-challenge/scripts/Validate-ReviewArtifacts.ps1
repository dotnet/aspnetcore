[CmdletBinding()]
param(
    [Parameter(Mandatory, Position = 0)]
    [string] $ArtifactRoot
)

Set-StrictMode -Version 3.0
$ErrorActionPreference = 'Stop'

if (-not (Get-Command Join-Path -ErrorAction SilentlyContinue))
{
    $managementModule = [IO.Path]::Combine(
        $PSHOME,
        'Modules',
        'Microsoft.PowerShell.Management',
        'Microsoft.PowerShell.Management.psd1')
    Import-Module $managementModule -Global -Force
}

if (-not (Get-Command ForEach-Object -ErrorAction SilentlyContinue) -or
    -not (Get-Command Add-Member -ErrorAction SilentlyContinue))
{
    $utilityModule = [IO.Path]::Combine(
        $PSHOME,
        'Modules',
        'Microsoft.PowerShell.Utility',
        'Microsoft.PowerShell.Utility.psd1')
    Import-Module $utilityModule -Global -Force
}

$usePortableFileSystem = $false
try
{
    $expectedScriptRoot = [IO.Path]::GetFullPath($PSScriptRoot)
    $resolvedScriptRoot = (Resolve-Path -LiteralPath $PSScriptRoot -ErrorAction Stop).Path
    $usePortableFileSystem = $expectedScriptRoot -ne $resolvedScriptRoot
}
catch
{
    $usePortableFileSystem = $true
}

if ($usePortableFileSystem)
{
    # Some hosted PowerShell installations corrupt Unix paths through the provider layer.
    function global:Join-Path
    {
        [CmdletBinding()]
        param(
            [Parameter(Mandatory, Position = 0)]
            [string] $Path,

            [Parameter(Mandatory, Position = 1)]
            [string] $ChildPath
        )

        return [IO.Path]::Combine($Path, $ChildPath)
    }

    function global:Resolve-Path
    {
        [CmdletBinding()]
        param(
            [Parameter(Position = 0)]
            [string] $Path,

            [string] $LiteralPath
        )

        $value = if ($LiteralPath) { $LiteralPath } else { $Path }
        $fullPath = [IO.Path]::GetFullPath($value)
        if (-not [IO.File]::Exists($fullPath) -and -not [IO.Directory]::Exists($fullPath))
        {
            throw "Cannot find path '$value' because it does not exist."
        }

        $result = [pscustomobject]@{ Path = $fullPath }
        $result | Add-Member -MemberType ScriptMethod -Name ToString -Value { return $this.Path } -Force

        return $result
    }

    function global:Test-Path
    {
        [CmdletBinding()]
        param(
            [Parameter(Position = 0)]
            [string] $Path,

            [string] $LiteralPath,

            [object] $PathType
        )

        $value = if ($LiteralPath) { $LiteralPath } else { $Path }
        switch ([string] $PathType)
        {
            'Leaf' { return [IO.File]::Exists($value) }
            'Container' { return [IO.Directory]::Exists($value) }
            default { return [IO.File]::Exists($value) -or [IO.Directory]::Exists($value) }
        }
    }

    function global:Get-Content
    {
        [CmdletBinding()]
        param(
            [Parameter(Position = 0)]
            [string] $Path,

            [string] $LiteralPath,

            [switch] $Raw
        )

        $value = if ($LiteralPath) { $LiteralPath } else { $Path }
        if ($Raw)
        {
            return [IO.File]::ReadAllText($value)
        }

        return [IO.File]::ReadAllLines($value)
    }
}

Import-Module (Join-Path $PSScriptRoot 'ReviewArtifactTools.psm1') -Force -DisableNameChecking

$errors = @(Test-ReviewArtifacts -Root $ArtifactRoot)
if ($errors.Count -gt 0)
{
    $errors | ForEach-Object { Write-Error $_ }
    exit 1
}

Write-Host 'ASP.NET Core review artifacts are complete and calibrated.'
