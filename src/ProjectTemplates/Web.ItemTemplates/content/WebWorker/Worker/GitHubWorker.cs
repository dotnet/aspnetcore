// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// ============================================================================
// SAMPLE CODE - This file demonstrates how to create a WebWorker method.
// Feel free to delete this file and create your own worker classes.
// ============================================================================

using System.Net;
using System.Net.Http.Headers;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using System.Text.Json;
using WebWorkerTemplate.Models;

namespace WebWorkerTemplate.Worker;

/// <summary>
/// JSON processing worker that runs in a WebWorker.
/// Fetches data from GitHub API, deserializes it, computes metrics, and returns a compact result.
/// </summary>
[SupportedOSPlatform("browser")]
public static partial class GitHubWorker
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [JSImport("globalThis.postProgress")]
    private static partial void ReportProgress(string message, int current, int total);

    /// <summary>
    /// Fetches issues (including PRs) from a GitHub repository and computes metrics.
    /// Uses the GitHub Issues API which returns both issues and PRs.
    /// </summary>
    /// <param name="owner">Repository owner (e.g., "dotnet")</param>
    /// <param name="repo">Repository name (e.g., "runtime")</param>
    /// <param name="maxPages">Maximum number of pages to fetch (100 items per page). Default 5 = 500 items.</param>
    /// <param name="token">GitHub Personal Access Token for authentication (optional)</param>
    /// <returns>Serialized GitHubMetrics as JSON string</returns>
    [JSExport]
    public static async Task<string> FetchAndAnalyzeAsync(string owner, string repo, int maxPages = 5, string token = "")
    {
        var startTicks = Environment.TickCount64;

        using var httpClient = CreateGitHubClient(token);

        var allIssues = new List<GitHubIssue>();
        var totalBytes = 0;
        var pagesLoaded = 0;
        var deserializationTimeMs = 0.0;

        // Fetch multiple pages of issues (includes PRs)
        for (int page = 1; page <= maxPages; page++)
        {
            ReportProgress($"Fetching page {page} of {maxPages}...", page - 1, maxPages + 1);
            
            var url = $"https://api.github.com/repos/{owner}/{repo}/issues?state=all&per_page=100&page={page}";
            
            var response = await httpClient.GetAsync(url);
            
            if (response.StatusCode == HttpStatusCode.Forbidden)
            {
                // Rate limited - return what we have so far
                break;
            }
            
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync();
            totalBytes += json.Length * 2; // UTF-16 in .NET
            
            // Measure deserialization separately
            var deserializeStart = Environment.TickCount64;
            var issues = JsonSerializer.Deserialize<List<GitHubIssue>>(json, JsonOptions);
            deserializationTimeMs += Environment.TickCount64 - deserializeStart;
            
            if (issues == null || issues.Count == 0)
            {
                // No more data
                break;
            }
            
            allIssues.AddRange(issues);
            pagesLoaded++;
            
            // Check if we've reached the last page
            if (issues.Count < 100)
            {
                break;
            }
        }

        var totalElapsedMs = Environment.TickCount64 - startTicks;
        var fetchTimeMs = totalElapsedMs - deserializationTimeMs;

        if (allIssues.Count == 0)
        {
            throw new InvalidOperationException($"No issues found in {owner}/{repo}. The repository may be empty or rate-limited.");
        }

        // Compute metrics
        ReportProgress("Computing metrics...", maxPages, maxPages + 1);
        var computeStart = Environment.TickCount64;
        var metrics = ComputeMetrics(allIssues, owner, repo);
        var computeElapsedMs = Environment.TickCount64 - computeStart;

        metrics.FetchTimeMs = fetchTimeMs;
        metrics.DeserializationTimeMs = deserializationTimeMs;
        metrics.ComputationTimeMs = computeElapsedMs;
        metrics.JsonSizeBytes = totalBytes;
        metrics.PagesLoaded = pagesLoaded;

        var result = JsonSerializer.Serialize(metrics, JsonOptions);
        return result;
    }

    private static HttpClient CreateGitHubClient(string token = "")
    {
        var client = new HttpClient
        {
            // Set timeout for each individual HTTP request to prevent hanging
            Timeout = TimeSpan.FromSeconds(30)
        };
        
        // GitHub API requires User-Agent header
        client.DefaultRequestHeaders.UserAgent.Add(
            new ProductInfoHeaderValue("BlazorWebWorkerDemo", "1.0"));
        
        // Request JSON
        client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        
        // Use latest API version
        client.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
        
        // Add authentication if token provided
        if (!string.IsNullOrWhiteSpace(token))
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
        
        return client;
    }

    private static GitHubMetrics ComputeMetrics(List<GitHubIssue> issues, string owner, string repo)
    {
        var prs = issues.Where(i => i.IsPullRequest).ToList();
        var issuesOnly = issues.Where(i => !i.IsPullRequest).ToList();

        var metrics = new GitHubMetrics
        {
            Repository = $"{owner}/{repo}",
            GeneratedAt = DateTime.UtcNow,
            TotalItems = issues.Count,
            TotalPullRequests = prs.Count,
            TotalIssues = issuesOnly.Count,
            OpenItems = issues.Count(i => i.State == "open"),
            ClosedItems = issues.Count(i => i.State == "closed"),
            AverageComments = issues.Count > 0 ? issues.Average(i => i.Comments) : 0
        };

        // Top labels
        metrics.TopLabels = issues
            .SelectMany(i => i.Labels)
            .GroupBy(l => l.Name)
            .Select(g => new LabelCount { Label = g.Key, Count = g.Count() })
            .OrderByDescending(l => l.Count)
            .Take(15)
            .ToList();

        // Top authors
        metrics.TopAuthors = issues
            .Where(i => i.User != null)
            .GroupBy(i => i.User!.Login)
            .Select(g => new AuthorStats
            {
                Author = g.Key,
                PrCount = g.Count(i => i.IsPullRequest),
                IssueCount = g.Count(i => !i.IsPullRequest),
                TotalItems = g.Count()
            })
            .OrderByDescending(a => a.TotalItems)
            .Take(10)
            .ToList();

        // By milestone
        metrics.ByMilestone = issues
            .GroupBy(i => i.Milestone?.Title ?? "No Milestone")
            .ToDictionary(g => g.Key, g => g.Count());

        // By state
        metrics.ByState = issues
            .GroupBy(i => i.State)
            .ToDictionary(g => g.Key, g => g.Count());

        return metrics;
    }
}

