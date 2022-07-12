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
    private bool? _pendingNavigationLockState;

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

        if (_pendingNavigationLockState.HasValue)
        {
            SetHasLocationChangingListeners(_pendingNavigationLockState.Value);
            _pendingNavigationLockState = null;
        }
    }

    public void NotifyLocationChanged(string uri, string state, bool intercepted)
    {
        Log.ReceivedLocationChangedNotification(_logger, uri, intercepted);

        Uri = uri;
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
                Log.NavigationFailed(_logger, uri, exception);
                success = false;
            }
            else if (result.Canceled)
            {
                Log.NavigationCanceled(_logger, uri);
                success = false;
            }
            else
            {
                success = true;
            }

            _jsRuntime.InvokeVoidAsync(Interop.EndLocationChanging, callId, success).Preserve();
        });
    }

    /// <inheritdoc />
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(NavigationOptions))]
    protected override void NavigateToCore(string uri, NavigationOptions options)
    {
        Log.RequestingNavigation(_logger, uri, options);

        if (_jsRuntime == null)
        {
            var absoluteUriString = ToAbsoluteUri(uri).ToString();
            throw new NavigationException(absoluteUriString);
        }

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
                _jsRuntime.InvokeVoidAsync(Interop.NavigateTo, uri, options).Preserve();
            }
        });
    }

    protected override void SetNavigationLockState(bool value)
    {
        if (_jsRuntime is null)
        {
            _pendingNavigationLockState = value;
            return;
        }

        SetHasLocationChangingListeners(value);
    }

    private void SetHasLocationChangingListeners(bool value)
        => _jsRuntime.InvokeVoidAsync(Interop.SetHasLocationChangingListeners, value).Preserve();

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
    }
}
