$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$root = $PSScriptRoot
$documents = Join-Path $root 'documents'
$excerpts = Join-Path $root 'excerpts'

function Read-Json([string] $Name)
{
    Get-Content (Join-Path $documents $Name) -Raw | ConvertFrom-Json -AsHashtable
}

function Get-RequiredKey(
    [System.Collections.IDictionary] $Value,
    [string] $Key,
    [string] $Path)
{
    $matches = @($Value.Keys | Where-Object {
        [string]::Equals([string] $_, $Key, [StringComparison]::Ordinal)
    })
    $aliases = @($Value.Keys | Where-Object {
        [string]::Equals([string] $_, $Key, [StringComparison]::OrdinalIgnoreCase)
    })
    if ($matches.Count -ne 1 -or $aliases.Count -ne 1)
    {
        throw "Expected one unambiguous case-sensitive key '$Key' at '$Path'; found $($matches.Count) exact and $($aliases.Count) case-insensitive matches."
    }

    return $Value[$matches[0]]
}

function Get-RequiredPath(
    [System.Collections.IDictionary] $Value,
    [string[]] $Keys)
{
    $current = $Value
    $path = '$'
    foreach ($key in $Keys)
    {
        if ($current -isnot [System.Collections.IDictionary])
        {
            throw "Expected an object at '$path' before key '$key'."
        }

        $current = Get-RequiredKey $current $key $path
        $path = "$path.$key"
    }

    return $current
}

function ConvertTo-SortedValue([object] $Value)
{
    if ($Value -is [System.Collections.IDictionary])
    {
        $result = [ordered]@{}
        foreach ($key in @($Value.Keys) | Sort-Object { $_ } -CaseSensitive)
        {
            $result[$key] = ConvertTo-SortedValue $Value[$key]
        }

        return $result
    }

    if ($Value -is [System.Collections.IEnumerable] -and $Value -isnot [string])
    {
        return @($Value | ForEach-Object { ConvertTo-SortedValue $_ })
    }

    return $Value
}

function Get-Operation(
    [System.Collections.IDictionary] $Document,
    [string] $Path,
    [string] $Method)
{
    Get-RequiredPath $Document @('paths', $Path, $Method)
}

function Get-Components(
    [System.Collections.IDictionary] $Document,
    [string[]] $Names)
{
    $schemas = Get-RequiredPath $Document @('components', 'schemas')
    $result = [ordered]@{}
    foreach ($name in $Names)
    {
        $result[$name] = Get-RequiredKey $schemas $name '$.components.schemas'
    }

    return $result
}

function Get-SelectedProperties(
    [System.Collections.IDictionary] $Document,
    [string] $Component,
    [string[]] $Names)
{
    $properties = Get-RequiredPath $Document @('components', 'schemas', $Component, 'properties')
    $result = [ordered]@{}
    foreach ($name in $Names)
    {
        $result[$name] = Get-RequiredKey $properties $name "$.components.schemas.$Component.properties"
    }

    return $result
}

function Get-SelectedParameters(
    [System.Collections.IDictionary] $Document,
    [string[]] $Names)
{
    $parameters = Get-RequiredPath $Document @('paths', '/transport/{routeValue}', 'get', 'parameters')
    $byName = [System.Collections.Generic.Dictionary[string, object]]::new([StringComparer]::Ordinal)
    foreach ($parameter in $parameters)
    {
        $name = Get-RequiredKey $parameter 'name' '$.paths./transport/{routeValue}.get.parameters[]'
        if ($byName.ContainsKey($name))
        {
            throw "Duplicate transport parameter '$name'."
        }
        $byName.Add($name, $parameter)
    }

    $result = [ordered]@{}
    foreach ($name in $Names)
    {
        if (-not $byName.ContainsKey($name))
        {
            throw "Missing transport parameter '$name'."
        }
        $result[$name] = $byName[$name]
    }

    return $result
}

function Assert-SymmetricKeys(
    [System.Collections.IDictionary] $Left,
    [System.Collections.IDictionary] $Right,
    [string] $Label)
{
    $leftKeys = @($Left.Keys | Sort-Object { $_ } -CaseSensitive)
    $rightKeys = @($Right.Keys | Sort-Object { $_ } -CaseSensitive)
    if ([string]::Join("`n", $leftKeys) -cne [string]::Join("`n", $rightKeys))
    {
        throw "$Label must use identical case-sensitive selected key sets."
    }
}

function Write-Excerpt([string] $Name, [System.Collections.IDictionary] $Value)
{
    New-Item $excerpts -ItemType Directory -Force | Out-Null
    $json = ConvertTo-SortedValue $Value | ConvertTo-Json -Depth 100
    [System.IO.File]::WriteAllText(
        (Join-Path $excerpts $Name),
        $json + [Environment]::NewLine,
        [System.Text.UTF8Encoding]::new($false))
}

$legacy31 = Read-Json 'legacy-oas31.json'
$inferred31 = Read-Json 'inferred-oas31.json'
$legacy32 = Read-Json 'legacy-oas32.json'
$inferred32 = Read-Json 'inferred-oas32.json'
$inferred30 = Read-Json 'inferred-oas30.json'
$compatible31 = Read-Json 'inferred-oas31-compatibleonly.json'
$none31 = Read-Json 'inferred-oas31-none.json'
$callback31 = Read-Json 'inferred-oas31-callback.json'

Write-Excerpt '1-directional.json' ([ordered]@{
    legacy = [ordered]@{
        operation = Get-Operation $legacy31 '/directional' 'post'
        schemas = Get-Components $legacy31 @('DirectionalDto')
    }
    inferred = [ordered]@{
        operation = Get-Operation $inferred31 '/directional' 'post'
        schemas = Get-Components $inferred31 @('DirectionalDto.Input', 'DirectionalDto.Output')
    }
})

Write-Excerpt '2-inheritance-polymorphism.json' ([ordered]@{
    legacy = [ordered]@{
        inheritanceOperation = Get-Operation $legacy31 '/inheritance' 'get'
        polymorphismOperation = Get-Operation $legacy31 '/polymorphism' 'get'
        schemas = Get-Components $legacy31 @('Customer', 'Animal', 'AnimalCat', 'AnimalDog')
    }
    inferred = [ordered]@{
        inheritanceOperation = Get-Operation $inferred31 '/inheritance' 'get'
        polymorphismOperation = Get-Operation $inferred31 '/polymorphism' 'get'
        schemas = Get-Components $inferred31 @('Entity', 'Customer', 'Animal', 'AnimalCat', 'AnimalDog')
    }
})

$serializerNames = @('small', 'amount', 'data', 'identifier', 'relativeUri', 'name')
$legacySerializer = Get-SelectedProperties $legacy31 'SerializerEnvelope' $serializerNames
$inferredSerializer = Get-SelectedProperties $inferred31 'SerializerEnvelope' $serializerNames
Assert-SymmetricKeys $legacySerializer $inferredSerializer 'Serializer before/after excerpt'

Write-Excerpt '3-serializer.json' ([ordered]@{
    selectedProperties = $serializerNames
    legacy = [ordered]@{
        operation = Get-Operation $legacy31 '/serializer' 'post'
        properties = $legacySerializer
    }
    inferred = [ordered]@{
        operation = Get-Operation $inferred31 '/serializer' 'post'
        properties = $inferredSerializer
    }
})

$parameterNames = @(
    'routeValue',
    'count',
    'relativeUri',
    'identifier',
    'timestamp',
    'bigInteger',
    'address',
    'endpoint',
    'amount')
$legacyParameters = Get-SelectedParameters $legacy31 $parameterNames
$inferredParameters = Get-SelectedParameters $inferred31 $parameterNames
Assert-SymmetricKeys $legacyParameters $inferredParameters 'Transport before/after excerpt'

Write-Excerpt '4-scalars-transport.json' ([ordered]@{
    selectedProperties = $serializerNames
    selectedParameters = $parameterNames
    legacy31 = [ordered]@{
        parameters = $legacyParameters
        properties = $legacySerializer
    }
    inferred31 = [ordered]@{
        parameters = $inferredParameters
        properties = $inferredSerializer
    }
    legacy32 = [ordered]@{
        parameters = Get-SelectedParameters $legacy32 $parameterNames
        properties = Get-SelectedProperties $legacy32 'SerializerEnvelope' $serializerNames
    }
    inferred32 = [ordered]@{
        parameters = Get-SelectedParameters $inferred32 $parameterNames
        properties = Get-SelectedProperties $inferred32 'SerializerEnvelope' $serializerNames
    }
    inferred30 = [ordered]@{
        properties = Get-SelectedProperties $inferred30 'SerializerEnvelope' $serializerNames
    }
    compatibleOnly31 = [ordered]@{
        parameters = Get-SelectedParameters $compatible31 $parameterNames
        properties = Get-SelectedProperties $compatible31 'SerializerEnvelope' $serializerNames
    }
    none31 = [ordered]@{
        parameters = Get-SelectedParameters $none31 $parameterNames
        properties = Get-SelectedProperties $none31 'SerializerEnvelope' $serializerNames
    }
    callback31 = [ordered]@{
        parameters = Get-SelectedParameters $callback31 $parameterNames
        inputProperties = Get-SelectedProperties $callback31 'SerializerEnvelope.Input' $serializerNames
        outputProperties = Get-SelectedProperties $callback31 'SerializerEnvelope.Output' $serializerNames
    }
})

Write-Excerpt '5-tuple.json' ([ordered]@{
    legacy = [ordered]@{
        operation = Get-Operation $legacy31 '/tuple' 'get'
        schemas = Get-Components $legacy31 @('ValueTupleOflongAndboolean')
    }
    inferred = [ordered]@{
        operation = Get-Operation $inferred31 '/tuple' 'get'
        schemas = Get-Components $inferred31 @('ValueTupleOflongAndboolean')
    }
})

Write-Host "Wrote 5 deterministic normalized comparison files to $excerpts"
