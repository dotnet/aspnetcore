// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// ============================================================================
// SAMPLE CODE - These DTOs are used by the sample GitHubWorker.
// Feel free to delete this file and create your own model classes.
// ============================================================================

using System.Text.Json.Serialization;

namespace WebWorkerTemplate.Models;

/// <summary>
/// Represents a GitHub label.
/// </summary>
public sealed class GitHubLabel
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// Represents a GitHub issue (includes PRs in search results).
/// Maps to: GET /repos/{owner}/{repo}/issues
/// </summary>
public sealed class GitHubIssue
{
    [JsonPropertyName("state")]
    public string State { get; set; } = string.Empty;

    [JsonPropertyName("labels")]
    public List<GitHubLabel> Labels { get; set; } = [];
}

/// <summary>
/// Metrics computed from GitHub API data.
/// </summary>
public class GitHubMetrics
{
    public string Repository { get; set; } = string.Empty;
    public int TotalItems { get; set; }
    public int OpenItems { get; set; }
    public int ClosedItems { get; set; }
    
    // Performance
    public double FetchTimeMs { get; set; }
    public double DeserializationTimeMs { get; set; }
    public double ComputationTimeMs { get; set; }
    
    // Aggregations
    public List<LabelCount> TopLabels { get; set; } = [];
}

/// <summary>
/// Label count for metrics.
/// </summary>
public class LabelCount
{
    public string Label { get; set; } = string.Empty;
    public int Count { get; set; }
}
