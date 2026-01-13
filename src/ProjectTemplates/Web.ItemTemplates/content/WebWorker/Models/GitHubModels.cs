// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace WebWorkerTemplate.Models;

/// <summary>
/// Represents a pull request from GitHub API.
/// Maps to: GET /repos/{owner}/{repo}/pulls
/// </summary>
public sealed class GitHubPullRequest
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("number")]
    public int Number { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("state")]
    public string State { get; set; } = string.Empty;

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [JsonPropertyName("merged_at")]
    public DateTime? MergedAt { get; set; }

    [JsonPropertyName("draft")]
    public bool Draft { get; set; }

    [JsonPropertyName("user")]
    public GitHubUser? User { get; set; }

    [JsonPropertyName("labels")]
    public List<GitHubLabel> Labels { get; set; } = [];

    [JsonPropertyName("milestone")]
    public GitHubMilestone? Milestone { get; set; }

    [JsonPropertyName("requested_reviewers")]
    public List<GitHubUser> RequestedReviewers { get; set; } = [];

    // Note: additions, deletions, changed_files require fetching individual PR details
    // For efficiency, we'll skip these in the list endpoint
}

/// <summary>
/// Represents a GitHub user.
/// </summary>
public sealed class GitHubUser
{
    [JsonPropertyName("login")]
    public string Login { get; set; } = string.Empty;

    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("avatar_url")]
    public string AvatarUrl { get; set; } = string.Empty;
}

/// <summary>
/// Represents a GitHub label.
/// </summary>
public sealed class GitHubLabel
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("color")]
    public string Color { get; set; } = string.Empty;
}

/// <summary>
/// Represents a GitHub milestone.
/// </summary>
public sealed class GitHubMilestone
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("number")]
    public int Number { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("state")]
    public string State { get; set; } = string.Empty;
}

/// <summary>
/// Represents a GitHub issue (includes PRs in search results).
/// Maps to: GET /repos/{owner}/{repo}/issues
/// </summary>
public sealed class GitHubIssue
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("number")]
    public int Number { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("state")]
    public string State { get; set; } = string.Empty;

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [JsonPropertyName("closed_at")]
    public DateTime? ClosedAt { get; set; }

    [JsonPropertyName("user")]
    public GitHubUser? User { get; set; }

    [JsonPropertyName("labels")]
    public List<GitHubLabel> Labels { get; set; } = [];

    [JsonPropertyName("milestone")]
    public GitHubMilestone? Milestone { get; set; }

    [JsonPropertyName("comments")]
    public int Comments { get; set; }

    /// <summary>
    /// If this is set, the issue is actually a pull request.
    /// </summary>
    [JsonPropertyName("pull_request")]
    public GitHubPullRequestRef? PullRequest { get; set; }

    public bool IsPullRequest => PullRequest != null;
}

/// <summary>
/// Reference to a pull request (appears in issue responses).
/// </summary>
public sealed class GitHubPullRequestRef
{
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("merged_at")]
    public DateTime? MergedAt { get; set; }
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
    public int DraftCount { get; set; }
    public int MergedCount { get; set; }
    
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
    public List<AuthorStats> TopAuthors { get; set; } = [];
    public Dictionary<string, int> ByMilestone { get; set; } = [];
    public Dictionary<string, int> ByState { get; set; } = [];
}

/// <summary>
/// Label count for metrics.
/// </summary>
public class LabelCount
{
    public string Label { get; set; } = string.Empty;
    public int Count { get; set; }
}

/// <summary>
/// Author statistics for metrics.
/// </summary>
public class AuthorStats
{
    public string Author { get; set; } = string.Empty;
    public int PrCount { get; set; }
    public int IssueCount { get; set; }
    public int TotalItems { get; set; }
    public int CommitCount { get; set; }
}

