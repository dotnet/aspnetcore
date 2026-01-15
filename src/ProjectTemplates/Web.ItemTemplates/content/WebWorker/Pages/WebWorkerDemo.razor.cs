// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Net.Http.Headers;
using System.Runtime.Versioning;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using WebWorkerTemplate.Models;

namespace WebWorkerTemplate.Pages;

/// <summary>
/// Demo page comparing WebWorker vs UI thread processing.
/// </summary>
[SupportedOSPlatform("browser")]
public partial class WebWorkerDemo : ComponentBase
{
    [Inject]
    private HttpClient Http { get; set; } = default!;

    private string _repository = "dotnet/aspnetcore";
    private string _githubToken = "";
    private int _maxPages = 2;
    private int _clickCount;
    private bool _isProcessing;
    private string _processingMode = "";
    private bool _workerReady;
    private string _progressMessage = "";
    private string _errorMessage = "";
    private GitHubMetrics? _metricsWorker;
    private GitHubMetrics? _metricsUI;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await WorkerClient.WorkerClient.InitializeAsync();
            await WorkerClient.WorkerClient.WaitForReadyAsync();
            _workerReady = true;
            StateHasChanged();
        }
    }

    private void IncrementCounter()
    {
        _clickCount++;
    }

    private async Task FetchWithWorkerAsync()
    {
        if (!_workerReady || _isProcessing || string.IsNullOrWhiteSpace(_repository))
            return;

        var parts = _repository.Split('/');
        if (parts.Length != 2)
        {
            _errorMessage = "Invalid repository format. Use 'owner/repo' (e.g., 'dotnet/aspnetcore')";
            return;
        }

        _isProcessing = true;
        _processingMode = "worker";
        _errorMessage = "";
        _progressMessage = "Starting...";
        StateHasChanged();

        try
        {
            // Set up progress callback for this operation
            WorkerClient.WorkerClient.SetProgressCallback((message, current, total) =>
            {
                _progressMessage = $"{message} ({current}/{total})";
                InvokeAsync(StateHasChanged);
            });

            var owner = parts[0].Trim();
            var repo = parts[1].Trim();

            var json = await WorkerClient.WorkerClient.InvokeStringAsync(
                "WebWorkerTemplate.Worker.GitHubWorker.FetchAndAnalyzeAsync",
                TimeSpan.FromMinutes(2),
                owner,
                repo,
                _maxPages,
                _githubToken);

            _metricsWorker = JsonSerializer.Deserialize<GitHubMetrics>(json, JsonOptions);
            _progressMessage = "";
        }
        catch (Exception ex)
        {
            _errorMessage = GetUserFriendlyError(ex);
        }
        finally
        {
            _isProcessing = false;
            _processingMode = "";
            WorkerClient.WorkerClient.SetProgressCallback(null);
            StateHasChanged();
        }
    }

    private async Task FetchOnUIThreadAsync()
    {
        if (_isProcessing || string.IsNullOrWhiteSpace(_repository))
            return;

        var parts = _repository.Split('/');
        if (parts.Length != 2)
        {
            _errorMessage = "Invalid repository format. Use 'owner/repo' (e.g., 'dotnet/aspnetcore')";
            return;
        }

        try
        {
            _isProcessing = true;
            _processingMode = "ui";
            _errorMessage = "";
            StateHasChanged();

            await Task.Delay(50); // Allow UI to update

            var sw = Stopwatch.StartNew();

            var owner = parts[0].Trim();
            var repo = parts[1].Trim();

            // Set up HttpClient with GitHub headers
            Http.DefaultRequestHeaders.Clear();
            Http.DefaultRequestHeaders.UserAgent.Add(
                new ProductInfoHeaderValue("BlazorWebWorkerDemo", "1.0"));
            Http.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
            Http.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");

            if (!string.IsNullOrWhiteSpace(_githubToken))
            {
                Http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _githubToken);
            }

            var allIssues = new List<GitHubIssue>();
            var totalBytes = 0;
            var pagesLoaded = 0;
            var deserializationTimeMs = 0.0;

            for (int page = 1; page <= _maxPages; page++)
            {
                _progressMessage = $"Fetching page {page} of {_maxPages}...";
                StateHasChanged();
                await Task.Yield();

                var url = $"https://api.github.com/repos/{owner}/{repo}/issues?state=all&per_page=100&page={page}";
                var response = await Http.GetAsync(url);

                if ((int)response.StatusCode == 403)
                {
                    if (allIssues.Count == 0)
                        throw new InvalidOperationException("Rate limited by GitHub API. Try again later or use a token.");
                    break;
                }

                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                totalBytes += json.Length * 2;

                var deserializeSw = Stopwatch.StartNew();
                var issues = JsonSerializer.Deserialize<List<GitHubIssue>>(json, JsonOptions);
                deserializeSw.Stop();
                deserializationTimeMs += deserializeSw.Elapsed.TotalMilliseconds;

                if (issues == null || issues.Count == 0)
                    break;

                allIssues.AddRange(issues);
                pagesLoaded++;

                if (issues.Count < 100)
                    break;
            }

            _progressMessage = "Computing metrics...";
            StateHasChanged();
            await Task.Yield();

            sw.Stop();
            var fetchTimeMs = sw.Elapsed.TotalMilliseconds - deserializationTimeMs;

            if (allIssues.Count == 0)
            {
                throw new InvalidOperationException($"No data found in {_repository}");
            }

            sw.Restart();
            _metricsUI = ComputeMetrics(allIssues, owner, repo);
            sw.Stop();

            _metricsUI.FetchTimeMs = fetchTimeMs;
            _metricsUI.DeserializationTimeMs = deserializationTimeMs;
            _metricsUI.ComputationTimeMs = sw.Elapsed.TotalMilliseconds;
            _metricsUI.JsonSizeBytes = totalBytes;
            _metricsUI.PagesLoaded = pagesLoaded;
        }
        catch (Exception ex)
        {
            _errorMessage = GetUserFriendlyError(ex);
        }
        finally
        {
            _isProcessing = false;
            _processingMode = "";
            _progressMessage = "";
            StateHasChanged();
        }
    }

    private static GitHubMetrics ComputeMetrics(List<GitHubIssue> issues, string owner, string repo)
    {
        var metrics = new GitHubMetrics
        {
            Repository = $"{owner}/{repo}",
            GeneratedAt = DateTime.UtcNow,
            TotalItems = issues.Count,
            OpenItems = issues.Count(i => i.State == "open"),
            ClosedItems = issues.Count(i => i.State == "closed"),
            AverageComments = issues.Count > 0 ? issues.Average(i => i.Comments) : 0
        };

        metrics.TopLabels = issues
            .SelectMany(i => i.Labels)
            .GroupBy(l => l.Name)
            .Select(g => new LabelCount { Label = g.Key, Count = g.Count() })
            .OrderByDescending(l => l.Count)
            .Take(15)
            .ToList();

        metrics.TopAuthors = issues
            .Where(i => i.User != null)
            .GroupBy(i => i.User!.Login)
            .Select(g => new AuthorStats
            {
                Author = g.Key,
                TotalItems = g.Count(),
                IssueCount = g.Count(i => !i.IsPullRequest),
                PrCount = g.Count(i => i.IsPullRequest)
            })
            .OrderByDescending(a => a.TotalItems)
            .Take(10)
            .ToList();

        return metrics;
    }

    private static string GetUserFriendlyError(Exception ex)
    {
        return ex.Message switch
        {
            var m when m.Contains("404") => "Repository not found. Check the owner/repo format.",
            var m when m.Contains("403") => "Rate limited. Try again later or add a GitHub token.",
            var m when m.Contains("401") => "Invalid GitHub token.",
            _ => $"Error: {ex.Message}"
        };
    }
}
