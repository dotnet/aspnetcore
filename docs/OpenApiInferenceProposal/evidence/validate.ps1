$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$root = $PSScriptRoot
$documents = Join-Path $root 'documents'
$validation = Join-Path $root 'validation'
$transcriptLines = [System.Collections.Generic.List[string]]::new()

function Read-Json([string] $Path)
{
    Get-Content $Path -Raw | ConvertFrom-Json -AsHashtable
}

function Write-Json([string] $Path, [object] $Value)
{
    $json = $Value | ConvertTo-Json -Depth 100
    [System.IO.File]::WriteAllText(
        $Path,
        $json + [Environment]::NewLine,
        [System.Text.UTF8Encoding]::new($false))
}

function Assert-True([bool] $Condition, [string] $Message)
{
    if (-not $Condition)
    {
        throw $Message
    }
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

function Assert-KeyAbsent(
    [System.Collections.IDictionary] $Value,
    [string] $Key,
    [string] $Path,
    [string] $Message)
{
    $matches = @($Value.Keys | Where-Object {
        [string]::Equals([string] $_, $Key, [StringComparison]::OrdinalIgnoreCase)
    })
    if ($matches.Count -ne 0)
    {
        throw "$Message Found key '$($matches[0])' matching '$Key' case-insensitively at '$Path'."
    }
}

function Get-Parameters(
    [System.Collections.IDictionary] $Document,
    [string] $Path)
{
    $parameters = Get-RequiredPath $Document @('paths', $Path, 'get', 'parameters')
    $result = [System.Collections.Generic.Dictionary[string, object]]::new([StringComparer]::Ordinal)
    foreach ($parameter in $parameters)
    {
        $name = Get-RequiredKey $parameter 'name' "$.paths.$Path.get.parameters[]"
        if ($result.ContainsKey($name))
        {
            throw "Duplicate parameter '$name' at '$.paths.$Path.get.parameters'."
        }
        $result.Add($name, (Get-RequiredKey $parameter 'schema' "$.paths.$Path.get.parameters[$name]"))
    }

    return $result
}

function Assert-ExactKeys(
    [System.Collections.IDictionary] $Value,
    [string[]] $Expected,
    [string] $Message)
{
    $actual = @($Value.Keys | Sort-Object { $_ } -CaseSensitive)
    $expectedSorted = @($Expected | Sort-Object { $_ } -CaseSensitive)
    if ([string]::Join("`n", $actual) -cne [string]::Join("`n", $expectedSorted))
    {
        throw "$Message Expected [$([string]::Join(', ', $expectedSorted))], actual [$([string]::Join(', ', $actual))]."
    }
}

function Write-SchemaWrapper(
    [string] $Name,
    [string] $Reference,
    [System.Collections.IDictionary] $Document)
{
    $path = Join-Path $validation "$Name.schema.json"
    Write-Json $path ([ordered]@{
        '$schema' = 'https://json-schema.org/draft/2020-12/schema'
        '$ref' = $Reference
        components = Get-RequiredKey $Document 'components' '$'
    })
    return $path
}

function Invoke-CorvusValidation(
    [string] $Schema,
    [string] $Name,
    [object] $Payload,
    [bool] $ExpectedValid,
    [string] $Rationale)
{
    $payloadPath = Join-Path $validation "$Name.json"
    Write-Json $payloadPath $Payload
    $schemaRelative = "validation/$([IO.Path]::GetFileName($Schema))"
    $payloadRelative = "validation/$([IO.Path]::GetFileName($payloadPath))"
    $output = & dotnet tool run corvusjson -- validateDocument $schemaRelative $payloadRelative 2>&1 | Out-String
    if ($LASTEXITCODE -ne 0)
    {
        throw "Corvus validation for '$Name' failed with exit code $LASTEXITCODE.`n$output"
    }

    $valid = $output -match '(?m)^Document is valid\s*$'
    $invalid = $output -match '(?m)^Document is invalid\s*$'
    if ($valid -eq $invalid)
    {
        throw "Corvus validation for '$Name' did not return one unambiguous result.`n$output"
    }

    if ($valid -ne $ExpectedValid)
    {
        throw "Corvus validation for '$Name' returned an unexpected result.`n$output"
    }

    $expectation = $ExpectedValid ? 'valid' : 'invalid'
    $transcriptLines.Add("COMMAND: dotnet tool run corvusjson -- validateDocument $schemaRelative $payloadRelative")
    $transcriptLines.Add("EXPECTED: $expectation")
    $transcriptLines.Add("RATIONALE: $Rationale")
    $transcriptLines.Add("RESULT: $($valid ? 'valid' : 'invalid')")
    $sanitizedOutput = $output.Replace($root, '<evidence-root>', [StringComparison]::Ordinal)
    $transcriptLines.Add("OUTPUT:")
    foreach ($line in $sanitizedOutput.TrimEnd() -split "\r?\n")
    {
        $transcriptLines.Add("  $line")
    }
    $transcriptLines.Add('')

    $verb = $ExpectedValid ? 'accepted' : 'rejected'
    Write-Host "Corvus $verb $Name"
}

New-Item $validation -ItemType Directory -Force | Out-Null
Push-Location $root
try
{
    $transcriptLines.Add('Corvus.Json.Cli semantic spot-check transcript')
    $transcriptLines.Add('Package: Corvus.Json.Cli 5.6.1 (third-party review tool; local manifest)')
    $transcriptLines.Add('Dialect wrapper: JSON Schema 2020-12')
    $transcriptLines.Add('The OpenAPI container is parsed separately by Program.cs.')
    $transcriptLines.Add('')

    foreach ($path in Get-ChildItem $documents -Filter '*.json' | Sort-Object Name)
    {
        $documentValue = Read-Json $path.FullName
        Get-RequiredKey $documentValue 'openapi' '$' | Out-Null
        Get-RequiredKey $documentValue 'paths' '$' | Out-Null
        Get-RequiredPath $documentValue @('components', 'schemas') | Out-Null
        Write-Host "Parsed $($path.Name)"
    }

    $legacy31 = Read-Json (Join-Path $documents 'legacy-oas31.json')
    $legacy32 = Read-Json (Join-Path $documents 'legacy-oas32.json')
    $inferred30 = Read-Json (Join-Path $documents 'inferred-oas30.json')
    $document = Read-Json (Join-Path $documents 'inferred-oas31.json')
    $inferred32 = Read-Json (Join-Path $documents 'inferred-oas32.json')
    $compatible31 = Read-Json (Join-Path $documents 'inferred-oas31-compatibleonly.json')
    $none31 = Read-Json (Join-Path $documents 'inferred-oas31-none.json')
    $callback31 = Read-Json (Join-Path $documents 'inferred-oas31-callback.json')

    Assert-True (
        (Get-RequiredKey $legacy32 'openapi' '$') -eq '3.2.0'
    ) 'Legacy 3.2 document must declare OpenAPI 3.2.0.'
    Assert-True (
        (Get-RequiredKey $inferred32 'openapi' '$') -eq '3.2.0'
    ) 'Inferred 3.2 document must declare OpenAPI 3.2.0.'

    $directional = Get-RequiredPath $document @('paths', '/directional', 'post')
    Assert-True (
        (Get-RequiredPath $directional @('requestBody', 'content', 'application/json', 'schema', '$ref')).EndsWith('DirectionalDto.Input')
    ) 'Directional request must use the Input schema.'
    Assert-True (
        (Get-RequiredPath $directional @('responses', '200', 'content', 'application/json', 'schema', '$ref')).EndsWith('DirectionalDto.Output')
    ) 'Directional response must use the Output schema.'

    Assert-True (
        (Get-RequiredPath $document @('components', 'schemas', 'Customer', 'allOf'))[0].'$ref'.EndsWith('Entity')
    ) 'Customer must retain lossless inheritance composition.'

    $serializer = Get-RequiredPath $document @('components', 'schemas', 'SerializerEnvelope', 'properties')
    $small = Get-RequiredKey $serializer 'small' '$.components.schemas.SerializerEnvelope.properties'
    Assert-True (
        (Get-RequiredKey $small 'minimum' '$.components.schemas.SerializerEnvelope.properties.small') -eq -128 -and
        (Get-RequiredKey $small 'maximum' '$.components.schemas.SerializerEnvelope.properties.small') -eq 127
    ) 'sbyte must retain exact numeric-instance bounds.'
    $amount = Get-RequiredKey $serializer 'amount' '$.components.schemas.SerializerEnvelope.properties'
    Assert-KeyAbsent $amount 'format' '$.components.schemas.SerializerEnvelope.properties.amount' 'Inferred decimal must not claim the double format.'
    Assert-True (
        (Get-RequiredPath $serializer @('data', 'contentEncoding')) -eq 'base64'
    ) 'OpenAPI 3.1 byte arrays must retain the base64 contentEncoding annotation.'
    Assert-True (
        (Get-RequiredPath $inferred30 @('components', 'schemas', 'SerializerEnvelope', 'properties', 'data', 'format')) -eq 'byte'
    ) 'OpenAPI 3.0 byte arrays must retain the byte format annotation.'
    Assert-True (
        (Get-RequiredPath $inferred32 @('components', 'schemas', 'SerializerEnvelope', 'properties', 'data', 'contentEncoding')) -eq 'base64'
    ) 'OpenAPI 3.2 byte arrays must retain the base64 contentEncoding annotation.'
    Assert-True (
        (Get-RequiredPath $serializer @('identifier', 'format')) -eq 'uuid'
    ) 'Guid must use the conventional uuid format annotation.'
    Assert-True (
        (Get-RequiredPath $serializer @('relativeUri', 'format')) -eq 'uri-reference'
    ) 'Uri must use the conventional uri-reference format annotation.'

    $animal = Get-RequiredPath $document @('components', 'schemas', 'Animal')
    Assert-True (
        (Get-RequiredPath $animal @('discriminator', 'propertyName')) -eq 'kind'
    ) 'The OpenAPI discriminator annotation must name the kind property.'

    $parameters = Get-Parameters $document '/transport/{routeValue}'
    foreach ($name in @('relativeUri', 'identifier', 'timestamp', 'address', 'endpoint', 'routeValue'))
    {
        Assert-True $parameters.ContainsKey($name) "Missing required transport parameter '$name'."
    }
    $relativeUriParameter = Get-RequiredKey $parameters 'relativeUri' '$.paths./transport/{routeValue}.get.parameters'
    $identifierParameter = Get-RequiredKey $parameters 'identifier' '$.paths./transport/{routeValue}.get.parameters'
    $timestampParameter = Get-RequiredKey $parameters 'timestamp' '$.paths./transport/{routeValue}.get.parameters'
    $routeParameter = Get-RequiredKey $parameters 'routeValue' '$.paths./transport/{routeValue}.get.parameters'
    Assert-True (
        (Get-RequiredKey $relativeUriParameter 'format' '$.paths./transport/{routeValue}.get.parameters.relativeUri.schema') -eq 'uri-reference' -and
        (Get-RequiredKey $identifierParameter 'format' '$.paths./transport/{routeValue}.get.parameters.identifier.schema') -eq 'uuid' -and
        (Get-RequiredKey $timestampParameter 'format' '$.paths./transport/{routeValue}.get.parameters.timestamp.schema') -eq 'date-time'
    ) 'Conventional transport format annotations must be emitted.'
    foreach ($name in @('address', 'endpoint'))
    {
        Assert-ExactKeys $parameters[$name] @('type') "$name must remain a broad transport string without a format."
        Assert-True (
            (Get-RequiredKey $parameters[$name] 'type' "$.paths./transport/{routeValue}.get.parameters.$name.schema") -eq 'string'
        ) "$name must remain a broad transport string."
    }
    Assert-True (
        (Get-RequiredKey $routeParameter 'minimum' '$.paths./transport/{routeValue}.get.parameters.routeValue.schema') -eq -2147483648 -and
        (Get-RequiredKey $routeParameter 'maximum' '$.paths./transport/{routeValue}.get.parameters.routeValue.schema') -eq 2147483647
    ) 'Built-in int transport binding must retain exact numeric-instance bounds.'

    $compatibleSerializer = Get-RequiredPath $compatible31 @('components', 'schemas', 'SerializerEnvelope', 'properties')
    $compatibleParameters = Get-Parameters $compatible31 '/transport/{routeValue}'
    Assert-True (
        (Get-RequiredPath $compatibleSerializer @('identifier', 'format')) -eq 'uuid'
    ) 'CompatibleOnly must retain exact body uuid.'
    Assert-KeyAbsent (Get-RequiredKey $compatibleSerializer 'relativeUri' '$.components.schemas.SerializerEnvelope.properties') 'format' '$.components.schemas.SerializerEnvelope.properties.relativeUri' 'CompatibleOnly must suppress the conventional Uri format.'
    $compatibleIdentifier = Get-RequiredKey $compatibleParameters 'identifier' '$.paths./transport/{routeValue}.get.parameters'
    $compatibleRelativeUri = Get-RequiredKey $compatibleParameters 'relativeUri' '$.paths./transport/{routeValue}.get.parameters'
    $compatibleRoute = Get-RequiredKey $compatibleParameters 'routeValue' '$.paths./transport/{routeValue}.get.parameters'
    Assert-KeyAbsent $compatibleIdentifier 'format' '$.paths./transport/{routeValue}.get.parameters.identifier.schema' 'CompatibleOnly must suppress the transport Guid format.'
    Assert-KeyAbsent $compatibleRelativeUri 'format' '$.paths./transport/{routeValue}.get.parameters.relativeUri.schema' 'CompatibleOnly must suppress the transport Uri format.'
    Assert-True (
        (Get-RequiredKey $compatibleRoute 'format' '$.paths./transport/{routeValue}.get.parameters.routeValue.schema') -eq 'int32'
    ) 'CompatibleOnly must retain the established int32 client hint.'

    $noneSerializer = Get-RequiredPath $none31 @('components', 'schemas', 'SerializerEnvelope', 'properties')
    $noneParameters = Get-Parameters $none31 '/transport/{routeValue}'
    Assert-KeyAbsent (Get-RequiredKey $noneSerializer 'identifier' '$.components.schemas.SerializerEnvelope.properties') 'format' '$.components.schemas.SerializerEnvelope.properties.identifier' 'None must suppress body Guid format.'
    Assert-KeyAbsent (Get-RequiredKey $noneSerializer 'relativeUri' '$.components.schemas.SerializerEnvelope.properties') 'format' '$.components.schemas.SerializerEnvelope.properties.relativeUri' 'None must suppress body Uri format.'
    Assert-KeyAbsent (Get-RequiredKey $noneParameters 'routeValue' '$.paths./transport/{routeValue}.get.parameters') 'format' '$.paths./transport/{routeValue}.get.parameters.routeValue.schema' 'None must suppress transport width formats.'
    Assert-True (
        (Get-RequiredPath $noneSerializer @('data', 'contentEncoding')) -eq 'base64'
    ) 'None must not suppress proven base64 contentEncoding.'

    $callbackSerializer = Get-RequiredPath $callback31 @('components', 'schemas', 'SerializerEnvelope.Input', 'properties')
    $callbackParameters = Get-Parameters $callback31 '/transport/{routeValue}'
    Assert-True (
        (Get-RequiredPath $callbackSerializer @('identifier', 'format')) -eq 'guid-custom' -and
        (Get-RequiredKey (Get-RequiredKey $callbackParameters 'identifier' '$.paths./transport/{routeValue}.get.parameters') 'format' '$.paths./transport/{routeValue}.get.parameters.identifier.schema') -eq 'guid-custom'
    ) 'Callback replacement must apply to body and transport Guid contexts.'
    Assert-KeyAbsent (Get-RequiredKey $callbackSerializer 'relativeUri' '$.components.schemas.SerializerEnvelope.Input.properties') 'format' '$.components.schemas.SerializerEnvelope.Input.properties.relativeUri' 'Callback must suppress the body Uri format.'
    Assert-KeyAbsent (Get-RequiredKey $callbackParameters 'relativeUri' '$.paths./transport/{routeValue}.get.parameters') 'format' '$.paths./transport/{routeValue}.get.parameters.relativeUri.schema' 'Callback must suppress the transport Uri format.'

    Assert-True (
        (Get-RequiredPath $legacy31 @('components', 'schemas', 'SerializerEnvelope', 'properties', 'amount', 'format')) -eq 'double' -and
        (Get-RequiredPath $legacy31 @('components', 'schemas', 'SerializerEnvelope', 'properties', 'data', 'format')) -eq 'byte'
    ) 'Current Legacy scalar spot-checks changed unexpectedly.'
    Assert-True (
        (Get-RequiredPath $legacy32 @('components', 'schemas', 'SerializerEnvelope', 'properties', 'amount', 'format')) -eq 'double'
    ) 'Legacy OpenAPI 3.2 decimal must retain the current double annotation.'

    $scalarExcerpt = Read-Json (Join-Path $root 'excerpts/4-scalars-transport.json')
    $selectedProperties = Get-RequiredKey $scalarExcerpt 'selectedProperties' '$'
    $selectedParameters = Get-RequiredKey $scalarExcerpt 'selectedParameters' '$'
    foreach ($variant in @('legacy31', 'inferred31', 'legacy32', 'inferred32'))
    {
        $variantValue = Get-RequiredKey $scalarExcerpt $variant '$'
        Assert-ExactKeys (Get-RequiredKey $variantValue 'properties' "$.$variant") $selectedProperties "$variant scalar property selection is asymmetric."
        Assert-ExactKeys (Get-RequiredKey $variantValue 'parameters' "$.$variant") $selectedParameters "$variant transport parameter selection is asymmetric."
    }

    $animalSchema = Write-SchemaWrapper 'animal' '#/components/schemas/Animal' $document
    $directionalInputSchema = Write-SchemaWrapper 'directional-input' '#/components/schemas/DirectionalDto.Input' $document
    $serializerSchema = Write-SchemaWrapper 'serializer' '#/components/schemas/SerializerEnvelope' $document

    Invoke-CorvusValidation $animalSchema 'animal-cat-valid' ([ordered]@{
        kind = 'cat'
        name = 'Milo'
        lives = 9
    }) $true 'The cat branch satisfies its structural discriminator enum and member assertions.'
    Invoke-CorvusValidation $animalSchema 'animal-dog-valid' ([ordered]@{
        kind = 'dog'
        name = 'Rex'
        good = $true
    }) $true 'The dog branch satisfies its structural discriminator enum and member assertions.'
    Invoke-CorvusValidation $animalSchema 'animal-missing-discriminator-invalid' ([ordered]@{
        name = 'Missing discriminator'
    }) $false 'No oneOf branch accepts an object missing the structurally required discriminator property.'
    Invoke-CorvusValidation $animalSchema 'animal-unknown-discriminator-invalid' ([ordered]@{
        kind = 'bird'
        name = 'Unknown discriminator'
    }) $false 'No oneOf branch accepts an unknown discriminator literal.'

    Invoke-CorvusValidation $directionalInputSchema 'directional-input-valid' ([ordered]@{
        both = 'value'
        requiredButOmittable = $null
    }) $true 'The representative request contains the required input property.'
    Invoke-CorvusValidation $directionalInputSchema 'directional-input-missing-required-invalid' ([ordered]@{
        both = 'value'
    }) $false 'The representative request omits the structurally required input property.'

    Invoke-CorvusValidation $serializerSchema 'serializer-valid' ([ordered]@{
        small = '-12'
        amount = '123.45'
        data = 'AAEC/w=='
        identifier = '00112233-4455-6677-8899-aabbccddeeff'
        relativeUri = 'relative/path'
        name = 'sample'
        extension = [ordered]@{ nested = $true }
    }) $true 'The representative payload exercises quoted-number and extension-data structural branches.'
    Invoke-CorvusValidation $serializerSchema 'serializer-out-of-range-numeric-invalid' ([ordered]@{
        small = 128
        amount = 123.45
        data = 'AAEC/w=='
        identifier = '00112233-4455-6677-8899-aabbccddeeff'
        relativeUri = 'relative/path'
        name = 'sample'
    }) $false 'Numeric sbyte instances are constrained by minimum and maximum.'
    Invoke-CorvusValidation $serializerSchema 'serializer-out-of-range-quoted-known-underconstraint' ([ordered]@{
        small = '128'
        amount = 123.45
        data = 'AAEC/w=='
        identifier = '00112233-4455-6677-8899-aabbccddeeff'
        relativeUri = 'relative/path'
        name = 'sample'
    }) $true 'Known conservative underconstraint: the quoted-number branch models integer lexical shape, not the CLR sbyte range; System.Text.Json rejects this value as recorded by runtime-probe.json.'

    $runtimeProbe = Read-Json (Join-Path $validation 'runtime-probe.json')
    Assert-True (
        (Get-RequiredKey $runtimeProbe 'quotedSByte128' '$') -eq 'rejected-by-system-text-json'
    ) 'The public System.Text.Json runtime probe must reject quoted sbyte value 128.'

    $transcriptPath = Join-Path $validation 'corvus-transcript.txt'
    [IO.File]::WriteAllLines($transcriptPath, $transcriptLines, [Text.UTF8Encoding]::new($false))
    Write-Host 'Validated representative structural document invariants and annotation presence.'
    Write-Host 'Validated representative JSON Schema 2020-12 structural semantics with Corvus.Json.Cli 5.6.1.'
    Write-Host "Wrote sanitized Corvus transcript to $transcriptPath"
}
finally
{
    Pop-Location
}
