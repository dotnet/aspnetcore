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

        UpdateHasLocationChangingHandlers();
    }

    public void NotifyLocationChanged(string uri, string state, bool intercepted)
    {
        Log.ReceivedLocationChangedNotification(_logger, uri, intercepted);

        Uri = uri;
        HistoryEntryState = state;
        NotifyLocationChanged(intercepted);
    }

    public ValueTask<bool> HandleLocationChanging(string uri, bool intercepted, bool forceLoad)
    {
        return NotifyLocationChanging(uri, intercepted, forceLoad);
    }

    /// <inheritdoc />
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(NavigationOptions))]
    protected override async void NavigateToCore(string uri, NavigationOptions options)
    {
        Log.RequestingNavigation(_logger, uri, options);

        if (_jsRuntime == null)
        {
            var absoluteUriString = ToAbsoluteUri(uri).ToString();
            throw new NavigationException(absoluteUriString);
        }

        var shouldCancel = await NotifyLocationChanging(uri, false, options.ForceLoad);

        if (shouldCancel)
        {
            Log.NavigationCanceled(_logger, uri);
        }
        else
        {
            await _jsRuntime.InvokeVoidAsync(Interop.NavigateTo, uri, options);
        }
    }

    protected override bool SetHasLocationChangingHandlers(bool value)
    {
        if (_jsRuntime is null)
        {
            return false;
        }

        _jsRuntime.InvokeVoidAsync(Interop.SetHasLocationChangingListeners, value).Preserve();
        return true;
    }

    protected override async ValueTask EnableNavigationPromptAsync(string message, bool externalNavigationsOnly)
    {
        if (_jsRuntime is null)
        {
            // TODO: Would be better to handle this case more gracefully.
            throw new InvalidOperationException("The JavaScript runtime is not initialized.");
        }

        await _jsRuntime.InvokeVoidAsync(Interop.EnableNavigationPrompt, message, externalNavigationsOnly);
    }

    protected override async ValueTask DisableNavigationPromptAsync()
    {
        if (_jsRuntime is null)
        {
            // TODO: Would be better to handle this case more gracefully.
            throw new InvalidOperationException("The JavaScript runtime is not initialized.");
        }

        await _jsRuntime.InvokeVoidAsync(Interop.DisableNavigationPrompt);
    }

    private static partial class Log
    {
        [LoggerMessage(1, LogLevel.Debug, "Requesting navigation to URI {Uri} with forceLoad={ForceLoad}, replace={Replace}", EventName = "RequestingNavigation")]
        private static partial void RequestingNavigation(ILogger logger, string uri, bool forceLoad, bool replace);

        public static void RequestingNavigation(ILogger logger, string uri, NavigationOptions options)
            => RequestingNavigation(logger, uri, options.ForceLoad, options.ReplaceHistoryEntry);

        [LoggerMessage(2, LogLevel.Debug, "Received notification that the URI has changed to {Uri} with isIntercepted={IsIntercepted}", EventName = "ReceivedLocationChangedNotification")]
        public static partial void ReceivedLocationChangedNotification(ILogger logger, string uri, bool isIntercepted);

        [LoggerMessage(3, LogLevel.Debug, "Navigation canceled for URI {Uri}", EventName = "NavigationCanceled")]
        public static partial void NavigationCanceled(ILogger logger, string uri);
    }
}
