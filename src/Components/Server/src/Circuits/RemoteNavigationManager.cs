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
    private const string _enableThrowNavigationException = "Microsoft.AspNetCore.Components.Endpoints.HttpNavigationManager.EnableThrowNavigationException";
    private static bool _throwNavigationException =>
        AppContext.TryGetSwitch(_enableThrowNavigationException, out var switchValue) && switchValue;

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
            if (_throwNavigationException)
            {
                throw new NavigationException(absoluteUriString);
            }
            else
            {
                if (!IsInternalUri(absoluteUriString))
                {
                    // it's an external navigation, avoid Uri validation exception
                    BaseUri = GetBaseUriFromAbsoluteUri(absoluteUriString);
                }
                Uri = absoluteUriString;
                NotifyLocationChanged(isInterceptedLink: false);
                return;
            }
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

    private bool IsInternalUri(string uri)
    {
        var normalizedBaseUri = NormalizeBaseUri(BaseUri);
        return uri.StartsWith(normalizedBaseUri, StringComparison.OrdinalIgnoreCase);
    }

    private static string GetBaseUriFromAbsoluteUri(string absoluteUri)
    {
        // Find the position of the first single slash after the scheme (e.g., "https://")
        var schemeDelimiterIndex = absoluteUri.IndexOf("://", StringComparison.Ordinal);
        if (schemeDelimiterIndex == -1)
        {
            throw new ArgumentException($"The provided URI '{absoluteUri}' is not a valid absolute URI.");
        }

        // Find the end of the authority section (e.g., "https://example.com/")
        var authorityEndIndex = absoluteUri.IndexOf('/', schemeDelimiterIndex + 3);
        if (authorityEndIndex == -1)
        {
            // If no slash is found, the entire URI is the authority (e.g., "https://example.com")
            return NormalizeBaseUri(absoluteUri + "/");
        }

        // Extract the base URI up to the authority section
        return NormalizeBaseUri(absoluteUri.Substring(0, authorityEndIndex + 1));
    }

    private static string NormalizeBaseUri(string baseUri)
    {
        var lastSlashIndex = baseUri.LastIndexOf('/');
        if (lastSlashIndex >= 0)
        {
            baseUri = baseUri.Substring(0, lastSlashIndex + 1);
        }

        return baseUri;
    }

    /// <inheritdoc />
    public override void Refresh(bool forceReload = false)
    {
        if (_jsRuntime == null)
        {
            var absoluteUriString = ToAbsoluteUri(Uri).AbsoluteUri;
            if (_throwNavigationException)
            {
                throw new NavigationException(absoluteUriString);
            }
            else
            {
                Uri = absoluteUriString;
                NotifyLocationChanged(isInterceptedLink: false);
                return;
            }
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

        [LoggerMessage(1, LogLevel.Debug, "Requesting not found", EventName = "RequestingNotFound")]
        public static partial void RequestingNotFound(ILogger logger);

        [LoggerMessage(6, LogLevel.Debug, "Navigation completed when changing the location to {Uri}", EventName = "NavigationCompleted")]
        public static partial void NavigationCompleted(ILogger logger, string uri);

        [LoggerMessage(7, LogLevel.Debug, "Navigation stopped because the session ended when navigating to {Uri}", EventName = "NavigationStoppedSessionEnded")]
        public static partial void NavigationStoppedSessionEnded(ILogger logger, string uri);
    }
}
