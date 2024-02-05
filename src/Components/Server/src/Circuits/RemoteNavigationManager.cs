// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Interop = Microsoft.AspNetCore.Components.Web.BrowserNavigationManagerInterop;

namespace Microsoft.AspNetCore.Components.Server.Circuits;

/// <summary>
/// A Server-Side Blazor implementation of <see cref="NavigationManager"/>.
/// </summary>
internal sealed partial class RemoteNavigationManager : NavigationManager, IHostEnvironmentNavigationManager
{
    private readonly ILogger<RemoteNavigationManager> _logger;
    private IJSRuntime _jsRuntime;
    private bool? _navigationLockStateBeforeJsRuntimeAttached;

    public event EventHandler<Exception>? UnhandledException;

    /// <summary>
    /// Creates a new <see cref="RemoteNavigationManager"/> instance.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger{TCategoryName}"/>.</param>
    public RemoteNavigationManager(ILogger<RemoteNavigationManager> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Gets or sets whether the circuit has an attached <see cref="IJSRuntime"/>.
    /// </summary>
    public bool HasAttachedJSRuntime => _jsRuntime != null;

    /// <summary>
    /// Initializes the <see cref="NavigationManager" />.
    /// </summary>
    /// <param name="baseUri">The base URI.</param>
    /// <param name="uri">The absolute URI.</param>
    public new void Initialize(string baseUri, string uri)
    {
        base.Initialize(baseUri, uri);
        NotifyLocationChanged(isInterceptedLink: false);
    }

    /// <summary>
    /// Initializes the <see cref="RemoteNavigationManager"/>.
    /// </summary>
    /// <param name="jsRuntime">The <see cref="IJSRuntime"/> to use for interoperability.</param>
    public void AttachJsRuntime(IJSRuntime jsRuntime)
    {
        if (_jsRuntime != null)
        {
            throw new InvalidOperationException("JavaScript runtime already initialized.");
        }

        _jsRuntime = jsRuntime;

        if (_navigationLockStateBeforeJsRuntimeAttached.HasValue)
        {
            _ = SetHasLocationChangingListenersAsync(_navigationLockStateBeforeJsRuntimeAttached.Value);
            _navigationLockStateBeforeJsRuntimeAttached = null;
        }
    }

    public void NotifyLocationChanged(string uri, string state, bool intercepted)
    {
        Log.ReceivedLocationChangedNotification(_logger, uri, intercepted);

        Uri = uri;
        HistoryEntryState = state;
        NotifyLocationChanged(intercepted);
    }

    public async ValueTask<bool> HandleLocationChangingAsync(string uri, string? state, bool intercepted)
    {
        return await NotifyLocationChangingAsync(uri, state, intercepted);
    }

    /// <inheritdoc />
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(NavigationOptions))]
    protected override void NavigateToCore(string uri, NavigationOptions options)
    {
        Log.RequestingNavigation(_logger, uri, options);

        if (_jsRuntime == null)
        {
            var absoluteUriString = ToAbsoluteUri(uri).AbsoluteUri;
            throw new NavigationException(absoluteUriString);
        }

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

                await _jsRuntime.InvokeVoidAsync(Interop.NavigateTo, uri, options);
                Log.NavigationCompleted(_logger, uri);
            }
            catch (TaskCanceledException)
            when (_jsRuntime is RemoteJSRuntime remoteRuntime && remoteRuntime.IsPermanentlyDisconnected)
            {
                Log.NavigationStoppedSessionEnded(_logger, uri);
            }
            catch (Exception ex)
            {
                // We shouldn't ever reach this since exceptions thrown from handlers are handled in HandleLocationChangingHandlerException.
                // But if some other exception gets thrown, we still want to know about it.
                Log.NavigationFailed(_logger, uri, ex);
                UnhandledException?.Invoke(this, ex);
            }
        }
    }

    /// <inheritdoc />
    public override void Refresh(bool forceReload = false)
    {
        if (_jsRuntime == null)
        {
            var absoluteUriString = ToAbsoluteUri(Uri).AbsoluteUri;
            throw new NavigationException(absoluteUriString);
        }

        _ = RefreshAsync();

        async Task RefreshAsync()
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync(Interop.Refresh, forceReload);
            }
            catch (Exception ex)
            {
                Log.RefreshFailed(_logger, ex);
                UnhandledException?.Invoke(this, ex);
            }
        }
    }

    protected override void HandleLocationChangingHandlerException(Exception ex, LocationChangingContext context)
    {
        Log.NavigationFailed(_logger, context.TargetLocation, ex);
        UnhandledException?.Invoke(this, ex);
    }

    protected override void SetNavigationLockState(bool value)
    {
        if (_jsRuntime is null)
        {
            _navigationLockStateBeforeJsRuntimeAttached = value;
            return;
        }

        _ = SetHasLocationChangingListenersAsync(value);
    }

    private async Task SetHasLocationChangingListenersAsync(bool value)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync(Interop.SetHasLocationChangingListeners, WebRendererId.Server, value);
        }
        catch (JSDisconnectedException)
        {
            // If the browser is gone, we don't need it to clean up any browser-side state
        }
    }

    private static partial class Log
    {
        [LoggerMessage(1, LogLevel.Debug, "Requesting navigation to URI {Uri} with forceLoad={ForceLoad}, replace={Replace}", EventName = "RequestingNavigation")]
        private static partial void RequestingNavigation(ILogger logger, string uri, bool forceLoad, bool replace);

        public static void RequestingNavigation(ILogger logger, string uri, NavigationOptions options)
            => RequestingNavigation(logger, uri, options.ForceLoad, options.ReplaceHistoryEntry);

        [LoggerMessage(2, LogLevel.Debug, "Received notification that the URI has changed to {Uri} with isIntercepted={IsIntercepted}", EventName = "ReceivedLocationChangedNotification")]
        public static partial void ReceivedLocationChangedNotification(ILogger logger, string uri, bool isIntercepted);

        [LoggerMessage(3, LogLevel.Debug, "Navigation canceled when changing the location to {Uri}", EventName = "NavigationCanceled")]
        public static partial void NavigationCanceled(ILogger logger, string uri);

        [LoggerMessage(4, LogLevel.Error, "Navigation failed when changing the location to {Uri}", EventName = "NavigationFailed")]
        public static partial void NavigationFailed(ILogger logger, string uri, Exception exception);

        [LoggerMessage(5, LogLevel.Error, "Failed to refresh", EventName = "RefreshFailed")]
        public static partial void RefreshFailed(ILogger logger, Exception exception);

        [LoggerMessage(6, LogLevel.Debug, "Navigation completed when changing the location to {Uri}", EventName = "NavigationCompleted")]
        public static partial void NavigationCompleted(ILogger logger, string uri);

        [LoggerMessage(7, LogLevel.Debug, "Navigation stopped because the session ended when navigating to {Uri}", EventName = "NavigationStoppedSessionEnded")]
        public static partial void NavigationStoppedSessionEnded(ILogger logger, string uri);
    }
}
