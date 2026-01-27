// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// ============================================================================
// SAMPLE CODE - This page demonstrates WebWorker usage patterns.
// Feel free to delete this file and create your own pages.
// ============================================================================

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
    private int _maxPages = 2;
    private int _clickCount;
    private bool _isProcessing;
    private string _processingMode = "";
    private bool _workerReady;
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

    private void IncrementCounter() => _clickCount++;

    private async Task FetchAsync(bool useWorker)
    {
        if (useWorker && !_workerReady)
            return;
        if (_isProcessing || string.IsNullOrWhiteSpace(_repository))
            return;

        var parts = _repository.Split('/');
        if (parts.Length != 2)
        {
            _errorMessage = "Invalid repository format. Use 'owner/repo' (e.g., 'dotnet/aspnetcore')";
            return;
        }

        _isProcessing = true;
        _processingMode = useWorker ? "worker" : "ui";
        _errorMessage = "";
        StateHasChanged();

        try
        {
            var owner = parts[0].Trim();
            var repo = parts[1].Trim();

            string json;
            if (useWorker)
            {
                // Run on WebWorker thread
                json = await WorkerClient.WorkerClient.InvokeStringAsync(
                    "WebWorkerTemplate.Worker.GitHubWorker.FetchAndAnalyzeAsync",
                    TimeSpan.FromMinutes(2),
                    owner,
                    repo,
                    _maxPages);
            }
            else
            {
                // Run on UI thread - same code, different thread
                json = await Worker.GitHubWorker.FetchAndAnalyzeAsync(owner, repo, _maxPages);
            }

            var metrics = JsonSerializer.Deserialize<GitHubMetrics>(json, JsonOptions);
            if (useWorker)
                _metricsWorker = metrics;
            else
                _metricsUI = metrics;
        }
        catch (Exception ex)
        {
            _errorMessage = GetUserFriendlyError(ex);
        }
        finally
        {
            _isProcessing = false;
            _processingMode = "";
            StateHasChanged();
        }
    }

    private static string GetUserFriendlyError(Exception ex)
    {
        return ex.Message switch
        {
            var m when m.Contains("404") => "Repository not found. Check the owner/repo format.",
            var m when m.Contains("403") => "Rate limited by GitHub API. Try again later.",
            _ => $"Error: {ex.Message}"
        };
    }
}
