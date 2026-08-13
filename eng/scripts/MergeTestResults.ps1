param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('JUnit', 'XUnit')]
    [string]$Format,

    [Parameter(Mandatory = $true)]
    [string]$SearchPath,

    [Parameter(Mandatory = $true)]
    [string]$Filter,

    [Parameter(Mandatory = $true)]
    [string]$OutputPath,

    [switch]$Recurse,

    [switch]$RemoveSourceFiles
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 2

if (-not (Test-Path -LiteralPath $SearchPath)) {
    Write-Host "Test result search path '$SearchPath' does not exist."
    return
}

$outputFullPath = [System.IO.Path]::GetFullPath($OutputPath)
$files = @(
    Get-ChildItem -LiteralPath $SearchPath -Filter $Filter -File -Recurse:$Recurse |
        Where-Object { $_.FullName -ne $outputFullPath } |
        Sort-Object FullName
)

if ($files.Count -eq 0) {
    Write-Host "No $Format test result files matching '$Filter' were found under '$SearchPath'."
    return
}

if ($files.Count -eq 1) {
    if (Test-Path -LiteralPath $outputFullPath) {
        Remove-Item -LiteralPath $outputFullPath -Force
    }

    Write-Host "A single $Format test result file was found; no merge is needed."
    return
}

function Get-IntegerAttribute([System.Xml.Linq.XElement]$Element, [string]$Name) {
    $attribute = $Element.Attribute([System.Xml.Linq.XName]::Get($Name))
    if ($null -eq $attribute) {
        return 0L
    }

    return [long]::Parse($attribute.Value, [System.Globalization.CultureInfo]::InvariantCulture)
}

function Get-DoubleAttribute([System.Xml.Linq.XElement]$Element, [string]$Name) {
    $attribute = $Element.Attribute([System.Xml.Linq.XName]::Get($Name))
    if ($null -eq $attribute) {
        return 0.0
    }

    return [double]::Parse($attribute.Value, [System.Globalization.CultureInfo]::InvariantCulture)
}

$documents = @($files | ForEach-Object { [System.Xml.Linq.XDocument]::Load($_.FullName) })

if ($Format -eq 'XUnit') {
    $mergedRoot = [System.Xml.Linq.XElement]::new([System.Xml.Linq.XName]::Get('assemblies'))

    for ($documentIndex = 0; $documentIndex -lt $documents.Count; $documentIndex++) {
        $document = $documents[$documentIndex]
        if ($document.Root.Name.LocalName -ne 'assemblies') {
            throw "Expected an xUnit 'assemblies' root element in a $Format result file."
        }

        if ($documentIndex -eq 0) {
            foreach ($attribute in $document.Root.Attributes()) {
                $mergedRoot.SetAttributeValue($attribute.Name, $attribute.Value)
            }
        }

        foreach ($assembly in $document.Root.Elements()) {
            if ($assembly.Name.LocalName -eq 'assembly') {
                $mergedRoot.Add([System.Xml.Linq.XElement]::new($assembly))
            }
        }
    }
}
else {
    $mergedRoot = [System.Xml.Linq.XElement]::new([System.Xml.Linq.XName]::Get('testsuites'))
    $totals = @{
        tests = 0L
        failures = 0L
        errors = 0L
        skipped = 0L
        time = 0.0
    }

    foreach ($document in $documents) {
        if ($document.Root.Name.LocalName -eq 'testsuite') {
            $testSuites = @($document.Root)
        }
        elseif ($document.Root.Name.LocalName -eq 'testsuites') {
            $testSuites = @($document.Root.Elements() | Where-Object { $_.Name.LocalName -eq 'testsuite' })
        }
        else {
            throw "Expected a JUnit 'testsuite' or 'testsuites' root element in a $Format result file."
        }

        foreach ($testSuite in $testSuites) {
            $mergedRoot.Add([System.Xml.Linq.XElement]::new($testSuite))
            $totals.tests += Get-IntegerAttribute $testSuite 'tests'
            $totals.failures += Get-IntegerAttribute $testSuite 'failures'
            $totals.errors += Get-IntegerAttribute $testSuite 'errors'
            $totals.skipped += Get-IntegerAttribute $testSuite 'skipped'
            $totals.time += Get-DoubleAttribute $testSuite 'time'
        }
    }

    $mergedRoot.SetAttributeValue([System.Xml.Linq.XName]::Get('name'), 'Merged test results')
    foreach ($name in @('tests', 'failures', 'errors', 'skipped')) {
        $mergedRoot.SetAttributeValue(
            [System.Xml.Linq.XName]::Get($name),
            $totals[$name].ToString([System.Globalization.CultureInfo]::InvariantCulture))
    }
    $mergedRoot.SetAttributeValue(
        [System.Xml.Linq.XName]::Get('time'),
        $totals.time.ToString('0.################', [System.Globalization.CultureInfo]::InvariantCulture))
}

$outputDirectory = Split-Path -Parent $outputFullPath
New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null

$mergedDocument = [System.Xml.Linq.XDocument]::new(
    [System.Xml.Linq.XDeclaration]::new('1.0', 'utf-8', $null),
    $mergedRoot)
$mergedDocument.Save($outputFullPath)

if ($RemoveSourceFiles) {
    $files | Remove-Item -Force
}

Write-Host "Merged $($files.Count) $Format test result files into '$outputFullPath'."
