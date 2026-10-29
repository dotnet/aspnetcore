#!/usr/bin/env pwsh
#Requires -Version 7.0

[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$IssueBodyPath,
    [Parameter(Mandatory)][string]$OutputPath,
    [Parameter(Mandatory)][string]$Token
)

$ErrorActionPreference = "Stop"
$utf8 = [Text.UTF8Encoding]::new($false, $true)

function Get-Sha256
{
    param([Parameter(Mandatory)][byte[]]$Bytes)

    return [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($Bytes)).ToLowerInvariant()
}

function Get-CanonicalPublishedBody
{
    param([Parameter(Mandatory)][string]$Body)

    $suffixPattern = '(?s)(?<suffix>\n\n<!-- gh-aw-workflow-id: pr-attention-pulse -->\n\n' +
        '<!-- gh-aw-agentic-workflow: ASP\.NET Core Scoped PR Attention Pulse(?:, engine: [^,\r\n]+, model: [^,\r\n]+)?, id: (?<agenticRun>[1-9][0-9]*), workflow_id: pr-attention-pulse, run: https://github\.com/dotnet/aspnetcore/actions/runs/\k<agenticRun> -->\n' +
        '<!-- gh-aw-execution: (?<provenance>\{[^\r\n]+\}) -->)\n?\z'
    $suffix = [regex]::Match($Body, $suffixPattern)
    if ($suffix.Success)
    {
        return $Body.Substring(0, $suffix.Index)
    }

    return $Body
}

function Get-HttpClient
{
    $client = [Net.Http.HttpClient]::new()
    $client.DefaultRequestHeaders.UserAgent.ParseAdd("dotnet-aspnetcore-pr-attention-pulse")
    $client.DefaultRequestHeaders.Authorization = [Net.Http.Headers.AuthenticationHeaderValue]::new("Bearer", $Token)
    $client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json")
    $client.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28")
    return $client
}

function Read-Json
{
    param(
        [Parameter(Mandatory)][Net.Http.HttpClient]$Client,
        [Parameter(Mandatory)][string]$Path
    )

    $response = $Client.GetAsync("https://api.github.com/$Path").GetAwaiter().GetResult()
    try
    {
        if (-not $response.IsSuccessStatusCode)
        {
            throw "GitHub API GET '$Path' failed with HTTP $([int]$response.StatusCode)."
        }

        $json = $response.Content.ReadAsStringAsync().GetAwaiter().GetResult()
        return $json | ConvertFrom-Json -Depth 100
    }
    finally
    {
        $response.Dispose()
    }
}

function Read-Bytes
{
    param(
        [Parameter(Mandatory)][Net.Http.HttpClient]$Client,
        [Parameter(Mandatory)][string]$Path
    )

    $response = $Client.GetAsync("https://api.github.com/$Path").GetAwaiter().GetResult()
    try
    {
        if (-not $response.IsSuccessStatusCode)
        {
            throw "GitHub API GET '$Path' failed with HTTP $([int]$response.StatusCode)."
        }

        return ,$response.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult()
    }
    finally
    {
        $response.Dispose()
    }
}

function Read-ArtifactCandidate
{
    param(
        [Parameter(Mandatory)][byte[]]$Bytes,
        [Parameter(Mandatory)][string]$CanonicalBody,
        [Parameter(Mandatory)][string]$ExpectedHash
    )

    $stream = [IO.MemoryStream]::new($Bytes, $false)
    try
    {
        $archive = [IO.Compression.ZipArchive]::new($stream, [IO.Compression.ZipArchiveMode]::Read, $false)
        try
        {
            $entries = @($archive.Entries)
            if ($entries.Count -ne 2 -or
                (@($entries.FullName | Sort-Object) -join ",") -cne "pulse-body.md,pulse-input.json")
            {
                throw "The Pulse evidence archive does not contain the two exact expected files."
            }

            $bodyEntry = $archive.GetEntry("pulse-body.md")
            $inputEntry = $archive.GetEntry("pulse-input.json")
            $bodyStream = $bodyEntry.Open()
            $inputStream = $inputEntry.Open()
            try
            {
                $bodyMemory = [IO.MemoryStream]::new()
                $inputMemory = [IO.MemoryStream]::new()
                try
                {
                    $bodyStream.CopyTo($bodyMemory)
                    $inputStream.CopyTo($inputMemory)
                    $body = $utf8.GetString($bodyMemory.ToArray())
                    $inputBytes = $inputMemory.ToArray()
                }
                finally
                {
                    $bodyMemory.Dispose()
                    $inputMemory.Dispose()
                }
            }
            finally
            {
                $bodyStream.Dispose()
                $inputStream.Dispose()
            }

            if (-not [string]::Equals($body, $CanonicalBody, [StringComparison]::Ordinal) -or
                (Get-Sha256 -Bytes $inputBytes) -cne $ExpectedHash)
            {
                return $null
            }

            return $utf8.GetString($inputBytes)
        }
        finally
        {
            $archive.Dispose()
        }
    }
    finally
    {
        $stream.Dispose()
    }
}

function Write-PreviousResult
{
    param(
        [Parameter(Mandatory)][string]$Status,
        [Parameter(Mandatory)][AllowEmptyString()][string]$Reason,
        [AllowNull()][object]$Pulse
    )

    $result = [ordered]@{
        status = $Status
        reason = $Reason
        pulse = $Pulse
    }
    [IO.File]::WriteAllText(
        $OutputPath,
        ($result | ConvertTo-Json -Depth 100 -Compress -EscapeHandling EscapeNonAscii) + "`n",
        [Text.UTF8Encoding]::new($false))
}

try
{
    $publishedBody = [IO.File]::ReadAllText($IssueBodyPath)
    $canonicalBody = Get-CanonicalPublishedBody -Body $publishedBody
    $identity = [regex]::Match(
        $canonicalBody,
        '(?m)^Repository: `dotnet/aspnetcore`; run ID: `(?<run>[1-9][0-9]*)`; attempt: `(?<attempt>[1-9][0-9]*)`\.\nJSON SHA-256: `(?<hash>[0-9a-f]{64})`\.$')
    if (-not $identity.Success)
    {
        throw "The published Pulse snapshot identity is missing or invalid."
    }
    $runId = $identity.Groups["run"].Value
    $runAttempt = $identity.Groups["attempt"].Value
    $expectedHash = $identity.Groups["hash"].Value

    $embedded = [regex]::Match(
        $canonicalBody,
        '(?ms)<summary>Snapshot JSON \(exact sanitized bytes\)</summary>\n\n(?<fence>`{3,})json\n(?<json>.*?)\n\k<fence>\n\n</details>\z')
    $json = $null
    if ($embedded.Success)
    {
        $json = $embedded.Groups["json"].Value
        $bytes = $utf8.GetBytes($json)
        if ((Get-Sha256 -Bytes $bytes) -cne $expectedHash)
        {
            throw "The embedded Pulse snapshot does not match its published checksum."
        }
    }
    else
    {
        $client = Get-HttpClient
        try
        {
            $attempt = Read-Json -Client $client -Path "repos/dotnet/aspnetcore/actions/runs/$runId/attempts/$runAttempt"
            if ([string]$attempt.id -cne $runId -or
                [string]$attempt.run_attempt -cne $runAttempt -or
                -not [string]::Equals([string]$attempt.repository.full_name, "dotnet/aspnetcore", [StringComparison]::Ordinal))
            {
                throw "The producing attempt identity does not match the published Pulse snapshot."
            }
            $artifacts = [Collections.Generic.List[object]]::new()
            $page = 1
            $expectedCount = $null
            do
            {
                $listing = Read-Json -Client $client -Path "repos/dotnet/aspnetcore/actions/runs/$runId/artifacts?per_page=100&page=$page"
                if ($listing.total_count -isnot [int] -and $listing.total_count -isnot [long] -or $listing.artifacts -isnot [array])
                {
                    throw "The Pulse artifact listing is invalid."
                }
                if ($null -eq $expectedCount)
                {
                    $expectedCount = [int]$listing.total_count
                }
                elseif ($expectedCount -ne [int]$listing.total_count)
                {
                    throw "The Pulse artifact listing changed during pagination."
                }
                if (@($listing.artifacts).Count -eq 0 -and $artifacts.Count -lt $expectedCount)
                {
                    throw "The Pulse artifact listing ended before its advertised total."
                }
                foreach ($artifact in @($listing.artifacts))
                {
                    $artifacts.Add($artifact)
                }
                $page++
            }
            while ($artifacts.Count -lt $expectedCount)

            $matches = [Collections.Generic.List[string]]::new()
            foreach ($artifact in @($artifacts | Where-Object {
                [string]::Equals([string]$_.name, "pulse-publication-evidence", [StringComparison]::Ordinal) -and
                $_.expired -eq $false
            }))
            {
                if ([string]$artifact.workflow_run.id -cne $runId)
                {
                    throw "The Pulse artifact does not belong to the published producing run."
                }
                $bytes = Read-Bytes -Client $client -Path "repos/dotnet/aspnetcore/actions/artifacts/$($artifact.id)/zip"
                $candidate = Read-ArtifactCandidate -Bytes $bytes -CanonicalBody $canonicalBody -ExpectedHash $expectedHash
                if ($null -ne $candidate)
                {
                    $matches.Add($candidate)
                }
            }
            if ($matches.Count -ne 1)
            {
                throw "The published Pulse snapshot artifact could not be identified uniquely."
            }

            $json = $matches[0]
        }
        finally
        {
            $client.Dispose()
        }
    }

    $pulse = $json | ConvertFrom-Json -Depth 100
    if ($pulse.schemaVersion -cne "2.0.0")
    {
        Write-PreviousResult -Status "unavailable" -Reason "previous-snapshot-incompatible" -Pulse $null
    }
    else
    {
        Write-PreviousResult -Status "available" -Reason "" -Pulse $pulse
    }
}
catch
{
    Write-Warning "Previous Pulse snapshot comparison is unavailable: $($_.Exception.Message)"
    Write-PreviousResult -Status "unavailable" -Reason "previous-snapshot-unavailable" -Pulse $null
}
