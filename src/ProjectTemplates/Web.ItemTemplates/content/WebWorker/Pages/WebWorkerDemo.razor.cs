using Microsoft.AspNetCore.Components;
using WebWorkerTemplate.Models;

namespace WebWorkerTemplate.Pages;

public partial class WebWorkerDemo : ComponentBase
{
    private string _repository = "dotnet/aspnetcore";
    private string _githubToken = "";
    private int _maxPages = 2;
    private int _clickCount;
    private bool _isProcessing;
    private bool _isInitialized;
    private string _progressMessage = "";
    private string _errorMessage = "";
    private GitHubMetrics? _metrics;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            // Initialize the WebWorker client (static, only needs to be done once)
            await WorkerClient.WorkerClient.InitializeAsync();
            WorkerClient.WorkerClient.SetProgressCallback((message, current, total) =>
            {
                _progressMessage = $"{message} ({current}/{total})";
                InvokeAsync(StateHasChanged);
            });
            _isInitialized = true;
            StateHasChanged();
        }
    }

    private void IncrementCounter()
    {
        _clickCount++;
    }

    private async Task FetchWithWorkerAsync()
    {
        if (!_isInitialized || _isProcessing || string.IsNullOrWhiteSpace(_repository))
            return;

        var parts = _repository.Split('/');
        if (parts.Length != 2)
        {
            _errorMessage = "Invalid repository format. Use 'owner/repo' (e.g., 'dotnet/aspnetcore')";
            return;
        }

        _isProcessing = true;
        _errorMessage = "";
        _progressMessage = "Starting...";
        _metrics = null;
        StateHasChanged();

        try
        {
            var owner = parts[0].Trim();
            var repo = parts[1].Trim();

            // Call the worker method using full method path
            // Format: "Namespace.ClassName.MethodName"
            _metrics = await WorkerClient.WorkerClient.InvokeJsonAsync<GitHubMetrics>(
                "WebWorkerTemplate.Worker.GitHubWorker.FetchAndAnalyzeAsync",
                TimeSpan.FromMinutes(2),
                owner,
                repo,
                _maxPages,
                _githubToken);

            _progressMessage = "";
        }
        catch (Exception ex)
        {
            _errorMessage = $"Error: {ex.Message}";
        }
        finally
        {
            _isProcessing = false;
            WorkerClient.WorkerClient.SetProgressCallback(null);
            StateHasChanged();
        }
    }
}
