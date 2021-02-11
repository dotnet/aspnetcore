// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PlaywrightSharp;

namespace Microsoft.AspNetCore.BrowserTesting
{
    public class PageInformation : IDisposable
    {
        private readonly Page _page;
        private readonly ILogger<PageInformation> _logger;

        public List<string> FailedRequests { get; } = new();

        public List<LogEntry> BrowserConsoleLogs { get; } = new();

        public List<string> PageErrors { get; } = new();

        public List<IWebSocket> WebSockets { get; set; } = new();

        public PageInformation(Page page, ILogger<PageInformation> logger)
        {
            page.Console += RecordConsoleMessage;
            page.PageError += RecordPageError;
            page.RequestFailed += RecordFailedRequest;
            page.WebSocket += CaptureWebSocket;
            _page = page;
            _logger = logger;

            _  = LogPageVideoPath();
        }

        private void CaptureWebSocket(object sender, WebSocketEventArgs e)
        {
            WebSockets.Add(e.WebSocket);
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

        public void Dispose()
        {
            _page.Console -= RecordConsoleMessage;
            _page.PageError -= RecordPageError;
            _page.RequestFailed -= RecordFailedRequest;
        }

        private void RecordFailedRequest(object sender, RequestFailedEventArgs e)
        {
            try
            {
                _logger.LogError(e.FailureText);
            }
            catch
            {
            }
            FailedRequests.Add(e.FailureText);
        }

        private void RecordPageError(object sender, PageErrorEventArgs e)
        {
            // There needs to be a bit of experimentation with this, but message should be a good start.
            try
            {
                _logger.LogError(e.Message);
            }
            catch
            {
            }

            PageErrors.Add(e.Message);
        }

        private void RecordConsoleMessage(object sender, ConsoleEventArgs e)
        {
            var message = e.Message;
            var messageText = message.Text.Replace(Environment.NewLine, $"{Environment.NewLine}      ");
            var location = message.Location;

            var logMessage = $"[{_page.Url}]{Environment.NewLine}      {messageText}{Environment.NewLine}      ({location.URL}:{location.LineNumber}:{location.ColumnNumber})";

            try
            {
                _logger.Log(MapLogLevel(message.Type), logMessage);
            }
            catch
            {

                throw;
            }

            BrowserConsoleLogs.Add(new LogEntry(messageText, message.Type));

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
