// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Components.WebView.Services;

internal sealed partial class WebViewNavigationManager : NavigationManager
{
    private readonly ILogger _logger;
    private IpcSender _ipcSender;

    public WebViewNavigationManager(ILogger<WebViewNavigationManager> logger)
    {
        _logger = logger;
    }

    public void AttachToWebView(IpcSender ipcSender, string baseUrl, string initialUrl)
    {
        _ipcSender = ipcSender;
        Initialize(baseUrl, initialUrl);
    }

    public void LocationUpdated(string newUrl, string? state, bool intercepted)
    {
        Uri = newUrl;
        HistoryEntryState = state;
        NotifyLocationChanged(intercepted);
    }

    public void HandleLocationChanging(int callId, string uri, bool intercepted)
    {
        NotifyLocationChanging(uri, intercepted, result =>
        {
            bool success;

            if (result.Exception is { } exception)
            {
                success = false;
                Log.NavigationFailed(_logger, uri, exception);
            }
            else if (result.Canceled)
            {
                success = false;
                Log.NavigationCanceled(_logger, uri);
            }
            else
            {
                success = true;
            }

            _ipcSender.EndLocationChanging(callId, success);
        });
    }

    protected override void NavigateToCore(string uri, NavigationOptions options)
    {
        NotifyLocationChanging(uri, false, result =>
        {
            if (result.Exception is { } exception)
            {
                Log.NavigationFailed(_logger, uri, exception);
            }
            else if (result.Canceled)
            {
                Log.NavigationCanceled(_logger, uri);
            }
            else
            {
                _ipcSender.Navigate(uri, options);
            }
        });
    }

    private static partial class Log
    {
        [LoggerMessage(1, LogLevel.Debug, "Navigation canceled when changing the location to {Uri}", EventName = "NavigationCanceled")]
        public static partial void NavigationCanceled(ILogger logger, string uri);

        [LoggerMessage(2, LogLevel.Error, "Navigation failed when changing the location to {Uri}", EventName = "NavigationFailed")]
        public static partial void NavigationFailed(ILogger logger, string uri, Exception exception);
    }
}
