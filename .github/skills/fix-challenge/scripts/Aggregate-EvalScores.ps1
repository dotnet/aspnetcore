[CmdletBinding(DefaultParameterSetName = 'Scores')]
param(
    [Parameter(Mandatory, Position = 0)]
    [string[]] $EvalPath,

    [Parameter(Mandatory, ParameterSetName = 'Scores')]
    [string] $Scores,

    [Parameter(Mandatory, ParameterSetName = 'Vally')]
    [string[]] $VallyResults
)

Set-StrictMode -Version 3.0
$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'ReviewerEvalTools.psm1') -Force

$EvalPath = @($EvalPath | ForEach-Object { $_ -split ',' } | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
if ($EvalPath.Count -eq 0)
{
    throw 'at least one eval path is required'
}

if ($PSCmdlet.ParameterSetName -eq 'Vally')
{
    $VallyResults = @($VallyResults | ForEach-Object { $_ -split ',' } | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
    if ($VallyResults.Count -eq 0)
    {
        throw 'at least one Vally result mapping is required'
    }
}

$scoreData = @{}
if ($PSCmdlet.ParameterSetName -eq 'Scores')
{
    $document = Read-JsonDocument $Scores
    foreach ($property in $document.PSObject.Properties)
    {
        $scoreData[$property.Name] = @{}
        foreach ($score in $property.Value.PSObject.Properties)
        {
            $scoreData[$property.Name][$score.Name] = [double]$score.Value
        }
    }
}
else
{
    $groups = @{}
    foreach ($argument in $VallyResults)
    {
        $parts = $argument -split '=', 2
        if ($parts.Count -ne 2 -or [string]::IsNullOrWhiteSpace($parts[0]) -or [string]::IsNullOrWhiteSpace($parts[1]))
        {
            throw "invalid -VallyResults value '$argument'; expected SKILL_NAME=RESULTS_JSONL"
        }
        if (-not $groups.ContainsKey($parts[0])) { $groups[$parts[0]] = [Collections.Generic.List[string]]::new() }
        $groups[$parts[0]].Add($parts[1])
    }
    foreach ($skill in $groups.Keys)
    {
        $parsed = Read-VallyScores -Paths @($groups[$skill]) -ExpectedSkillName $skill
        if ($parsed.Errors.Count -gt 0) { throw ($parsed.Errors -join [Environment]::NewLine) }
        $scoreData[$skill] = $parsed.Scores
    }
}

$output = [ordered]@{}
$documents = [ordered]@{}
foreach ($path in $EvalPath)
{
    $document = Read-VallyEvalDocument $path
    $skill = [string]$document.skill_name
    if ([string]::IsNullOrWhiteSpace($skill)) { throw "$path`: Vally spec must declare a name" }
    if (-not $documents.Contains($skill))
    {
        $documents[$skill] = [Collections.Generic.List[object]]::new()
    }
    foreach ($eval in @($document.evals))
    {
        $documents[$skill].Add($eval)
    }
}

foreach ($skill in $documents.Keys)
{
    if (-not $scoreData.ContainsKey($skill)) { throw "$skill`: scores must be provided" }
    $document = [pscustomobject]@{ skill_name = $skill; evals = @($documents[$skill]) }
    $aggregate = Get-EvalScoreAggregate -Document $document -Scores $scoreData[$skill]
    if ($aggregate.Errors.Count -gt 0) { throw ("$skill`: " + ($aggregate.Errors -join '; ')) }
    $output[$skill] = $aggregate.Result
}

$output | ConvertTo-Json -Depth 10
