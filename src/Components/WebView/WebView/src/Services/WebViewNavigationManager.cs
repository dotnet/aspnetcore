// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Routing;
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

    public void HandleLocationChanging(int callId, string uri, string? state, bool intercepted)
    {
        _ = HandleLocationChangingAsync();

        async Task HandleLocationChangingAsync()
        {
            try
            {
                var shouldContinueNavigation = await NotifyLocationChangingAsync(uri, state, intercepted);

                if (!shouldContinueNavigation)
                {
                    Log.NavigationCanceled(_logger, uri);
                }

                _ipcSender.EndLocationChanging(callId, shouldContinueNavigation);
            }
            catch (Exception ex)
            {
                // We shouldn't ever reach this since exceptions thrown from handlers are caught in InvokeLocationChangingHandlerAsync.
                // But if some other exception gets thrown, we still want to know about it.
                Log.NavigationFailed(_logger, uri, ex);
                _ipcSender.NotifyUnhandledException(ex);
            }
        }
    }

    protected override void NavigateToCore(string uri, NavigationOptions options)
    {
        _ = PerformNavigationAsync();

        async Task PerformNavigationAsync()
        {
            try
            {
                var shouldContinueNavigation = await NotifyLocationChangingAsync(uri, options.HistoryEntryState, false);

                if (!shouldContinueNavigation)
                {
                    Log.NavigationCanceled(_logger, uri);
                    return;
                }

                _ipcSender.Navigate(uri, options);
            }
            catch (Exception ex)
            {
                // We shouldn't ever reach this since exceptions thrown from handlers are handled in HandleLocationChangingHandlerException.
                // But if some other exception gets thrown, we still want to know about it.
                Log.NavigationFailed(_logger, uri, ex);
                _ipcSender.NotifyUnhandledException(ex);
            }
        }
    }

    /// <inheritdoc />
    public override void Refresh(bool forceReload = false)
    {
        _ipcSender.Refresh(forceReload);
    }

    protected override void HandleLocationChangingHandlerException(Exception ex, LocationChangingContext context)
    {
        Log.NavigationFailed(_logger, context.TargetLocation, ex);
        _ipcSender.NotifyUnhandledException(ex);
    }

    protected override void SetNavigationLockState(bool value)
    {
        _ipcSender.SetHasLocationChangingListeners(value);
    }

    private static partial class Log
    {
        [LoggerMessage(1, LogLevel.Debug, "Navigation canceled when changing the location to {Uri}", EventName = "NavigationCanceled")]
        public static partial void NavigationCanceled(ILogger logger, string uri);

        [LoggerMessage(2, LogLevel.Error, "Navigation failed when changing the location to {Uri}", EventName = "NavigationFailed")]
        public static partial void NavigationFailed(ILogger logger, string uri, Exception exception);
    }
}
