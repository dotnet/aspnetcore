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

    /// <summary>
    /// Fetches issues (including PRs) from a GitHub repository and computes metrics.
    /// Uses the GitHub Issues API which returns both issues and PRs.
    /// </summary>
    /// <param name="owner">Repository owner (e.g., "dotnet")</param>
    /// <param name="repo">Repository name (e.g., "runtime")</param>
    /// <param name="maxPages">Maximum number of pages to fetch (100 items per page). Default 5 = 500 items.</param>
    /// <returns>Serialized GitHubMetrics as JSON string</returns>
    [JSExport]
    public static async Task<string> FetchAndAnalyzeAsync(string owner, string repo, int maxPages = 5)
    {
        var startTicks = Environment.TickCount64;

        using var httpClient = CreateGitHubClient();

        var allIssues = new List<GitHubIssue>();
        var deserializationTimeMs = 0.0;

        // Fetch multiple pages of issues (includes PRs)
        for (int page = 1; page <= maxPages; page++)
        {
            var url = $"https://api.github.com/repos/{owner}/{repo}/issues?state=all&per_page=100&page={page}";            
            var response = await httpClient.GetAsync(url);
            
            if (response.StatusCode == HttpStatusCode.Forbidden)
            {
                // Rate limited - return what we have so far
                break;
            }
            
            response.EnsureSuccessStatusCode();            
            var json = await response.Content.ReadAsStringAsync();
            
            // Measure deserialization separately
            var deserializeStart = Environment.TickCount64;
            var issues = JsonSerializer.Deserialize<List<GitHubIssue>>(json, JsonOptions);
            deserializationTimeMs += Environment.TickCount64 - deserializeStart;
            
            if (issues == null || issues.Count == 0)
            {
                break;
            }
            
            allIssues.AddRange(issues);
            
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
        var computeStart = Environment.TickCount64;
        var metrics = ComputeMetrics(allIssues, owner, repo);
        var computeElapsedMs = Environment.TickCount64 - computeStart;

        metrics.FetchTimeMs = fetchTimeMs;
        metrics.DeserializationTimeMs = deserializationTimeMs;
        metrics.ComputationTimeMs = computeElapsedMs;

        return JsonSerializer.Serialize(metrics, JsonOptions);
    }

    private static HttpClient CreateGitHubClient() => new()
    {
        Timeout = TimeSpan.FromSeconds(30),
        DefaultRequestHeaders =
        {
            UserAgent = { new ProductInfoHeaderValue("BlazorWebWorkerDemo", "1.0") },
            Accept = { new MediaTypeWithQualityHeaderValue("application/vnd.github+json") }
        }
    };

    private static GitHubMetrics ComputeMetrics(List<GitHubIssue> issues, string owner, string repo)
    {
        var metrics = new GitHubMetrics
        {
            Repository = $"{owner}/{repo}",
            TotalItems = issues.Count,
            OpenItems = issues.Count(i => i.State == "open"),
            ClosedItems = issues.Count(i => i.State == "closed"),
        };

        metrics.TopLabels = issues
            .SelectMany(i => i.Labels)
            .GroupBy(l => l.Name)
            .Select(g => new LabelCount { Label = g.Key, Count = g.Count() })
            .OrderByDescending(l => l.Count)
            .Take(15)
            .ToList();

        return metrics;
    }
}

