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

    public async ValueTask HandleLocationChangingAsync(int callId, string uri, bool intercepted)
    {
        bool shouldContinueNavigation;

        try
        {
            shouldContinueNavigation = await NotifyLocationChangingAsync(uri, intercepted);

            if (!shouldContinueNavigation)
            {
                Log.NavigationCanceled(_logger, uri);
            }
        }
        catch (Exception ex)
        {
            shouldContinueNavigation = false;
            Log.NavigationFailed(_logger, uri, ex);
        }

        _ipcSender.EndLocationChanging(callId, shouldContinueNavigation);
    }

    protected override void NavigateToCore(string uri, NavigationOptions options)
    {
        NotifyLocationChangingAsync(uri, false).AsTask().ContinueWith(t =>
        {
            if (t.Exception is { } exception)
            {
                Log.NavigationFailed(_logger, uri, exception);
            }
            else if (!t.Result)
            {
                Log.NavigationCanceled(_logger, uri);
            }
            else
            {
                _ipcSender.Navigate(uri, options);
            }
        }, TaskScheduler.Current);
    }

    private static partial class Log
    {
        [LoggerMessage(1, LogLevel.Debug, "Navigation canceled when changing the location to {Uri}", EventName = "NavigationCanceled")]
        public static partial void NavigationCanceled(ILogger logger, string uri);

        [LoggerMessage(2, LogLevel.Error, "Navigation failed when changing the location to {Uri}", EventName = "NavigationFailed")]
        public static partial void NavigationFailed(ILogger logger, string uri, Exception exception);
    }
}
