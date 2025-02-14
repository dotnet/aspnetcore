// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace Microsoft.AspNetCore.BrowserTesting;

public class PageInformation : IDisposable
{
    private readonly IPage _page;
    private readonly ILogger<PageInformation> _logger;

    public List<string> FailedRequests { get; } = new();

    public List<LogEntry> BrowserConsoleLogs { get; } = new();

    public List<string> PageErrors { get; } = new();

    public List<IWebSocket> WebSockets { get; set; } = new();

    public PageInformation(IPage page, ILogger<PageInformation> logger)
    {
        page.Console += RecordConsoleMessage;
        page.PageError += RecordPageError;
        page.RequestFailed += RecordFailedRequest;
        page.WebSocket += CaptureWebSocket;
        _page = page;
        _logger = logger;

        _ = LogPageVideoPath();
    }

    private void CaptureWebSocket(object sender, IWebSocket e)
    {
        WebSockets.Add(e);
    }

    private async Task LogPageVideoPath()
    {
        try
        {
            var path = _page.Video != null ? await _page.Video.PathAsync() : null;
            if (path != null)
            {
                _logger.LogInformation($"Page video recorded at: {path}");
            }
        }
        catch
        {
            // Silently swallow since we don't have a good way to report it and its not critical.
            throw;
        }
    }

    public void Dispose()
    {
        _page.Console -= RecordConsoleMessage;
        _page.PageError -= RecordPageError;
        _page.RequestFailed -= RecordFailedRequest;
    }

    private void RecordFailedRequest(object sender, IRequest e)
    {
        try
        {
            _logger.LogError(e.Failure);
        }
        catch
        {
        }
        FailedRequests.Add(e.Failure);
    }

    private void RecordPageError(object sender, string e)
    {
        // There needs to be a bit of experimentation with this, but message should be a good start.
        try
        {
            _logger.LogError(e);
        }
        catch
        {
        }

        PageErrors.Add(e);
    }

    private void RecordConsoleMessage(object sender, IConsoleMessage message)
    {
        try
        {
            var messageText = message.Text.Replace(Environment.NewLine, $"{Environment.NewLine}      ");
            var location = message.Location;

            var logMessage = $"[{_page.Url}]{Environment.NewLine}      {messageText}{Environment.NewLine}      ({location})";

            var logLevel = message.Type switch
            {
                "info" => LogLevel.Information,
                "verbose" => LogLevel.Debug,
                "warning" => LogLevel.Warning,
                "error" => LogLevel.Error,
                _ => LogLevel.Information
            };
            _logger.Log(logLevel, logMessage);

            BrowserConsoleLogs.Add(new LogEntry(messageText, message.Type));
        }
        catch
        {
            // Logging after the test is finished should not cause the test to fail
        }
    }

    public sealed class LogEntry
    {
        public string Message { get; }

        public string Level { get; }

        public LogEntry(string message, string level)
        {
            Message = message;
            Level = level;
        }
    }
}
