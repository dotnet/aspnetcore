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

    [JsonPropertyName("comments")]
    public int Comments { get; set; }

    /// <summary>
    /// If this is set, the issue is actually a pull request.
    /// </summary>
    [JsonPropertyName("pull_request")]
    public object? PullRequest { get; set; }

    public bool IsPullRequest => PullRequest != null;
}

/// <summary>
/// Metrics computed from GitHub API data.
/// </summary>
public class GitHubMetrics
{
    public string Repository { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
    
    // Item counts
    public int TotalItems { get; set; }
    public int TotalPullRequests { get; set; }
    public int TotalIssues { get; set; }
    public int OpenItems { get; set; }
    public int ClosedItems { get; set; }
    
    // Averages
    public double AverageComments { get; set; }
    
    // Performance
    public double FetchTimeMs { get; set; }
    public double DeserializationTimeMs { get; set; }
    public double ComputationTimeMs { get; set; }
    public int JsonSizeBytes { get; set; }
    public int PagesLoaded { get; set; }
    
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

