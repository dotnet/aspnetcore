// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PlaywrightSharp;

namespace Microsoft.AspNetCore.BrowserTesting
{
    public class PageInformation
    {
        private readonly Page _page;
        private readonly ILogger<PageInformation> _logger;

        public List<string> FailedRequests { get; } = new();

        public List<LogEntry> BrowserConsoleLogs { get; } = new();

        public List<string> PageErrors { get; } = new();

        public PageInformation(Page page, ILogger<PageInformation> logger)
        {
            page.Console += RecordConsoleMessage;
            page.PageError += RecordPageError;
            page.RequestFailed += RecordFailedRequest;
            _page = page;
            _logger = logger;

            _  = LogPageVideoPath();
        }

        private async Task LogPageVideoPath()
        {
            try
            {
                var path = _page.Video != null ? await _page.Video.GetPathAsync() : null;
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

        private void RecordFailedRequest(object sender, RequestFailedEventArgs e)
        {
            _logger.LogError(e.FailureText);
            FailedRequests.Add(e.FailureText);
        }

        private void RecordPageError(object sender, PageErrorEventArgs e)
        {
            // There needs to be a bit of experimentation with this, but message should be a good start.
            _logger.LogError(e.Message);
            PageErrors.Add(e.Message);
        }

        private void RecordConsoleMessage(object sender, ConsoleEventArgs e)
        {
            var message = e.Message;
            var location = message.Location;

            var logMessage = $"[{_page.Url}]{Environment.NewLine}      {message.Text}{Environment.NewLine}      ({location.URL}:{location.LineNumber}:{location.ColumnNumber})";
            _logger.Log(MapLogLevel(message.Type), logMessage);
            BrowserConsoleLogs.Add(new LogEntry(message.Text.Replace(Environment.NewLine, $"{Environment.NewLine}      "), message.Type));

            LogLevel MapLogLevel(string messageType) => messageType switch
            {
                "info" => LogLevel.Information,
                "verbose" => LogLevel.Debug,
                "warning" => LogLevel.Warning,
                "error" => LogLevel.Error,
                _ => LogLevel.Information
            };
        }

        public record LogEntry(string Message, string Level);
    }
}
