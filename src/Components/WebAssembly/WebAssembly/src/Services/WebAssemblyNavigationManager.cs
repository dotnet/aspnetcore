// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Interop = Microsoft.AspNetCore.Components.Web.BrowserNavigationManagerInterop;

namespace Microsoft.AspNetCore.Components.WebAssembly.Services;

/// <summary>
/// Default client-side implementation of <see cref="NavigationManager"/>.
/// </summary>
internal sealed partial class WebAssemblyNavigationManager : NavigationManager
{
    private ILogger<WebAssemblyNavigationManager> _logger = default!;

    /// <summary>
    /// Gets the instance of <see cref="WebAssemblyNavigationManager"/>.
    /// </summary>
    public static WebAssemblyNavigationManager Instance { get; set; } = default!;

    public WebAssemblyNavigationManager(string baseUri, string uri)
    {
        Initialize(baseUri, uri);
    }

    public void CreateLogger(ILoggerFactory loggerFactory)
    {
        if (_logger is not null)
        {
            throw new InvalidOperationException($"The {nameof(WebAssemblyNavigationManager)} has already created a logger.");
        }

        _logger = loggerFactory.CreateLogger<WebAssemblyNavigationManager>();
    }

    public void SetLocation(string uri, string? state, bool isInterceptedLink)
    {
        Uri = uri;
        HistoryEntryState = state;
        NotifyLocationChanged(isInterceptedLink);
    }

    public async ValueTask<bool> HandleLocationChangingAsync(string uri, bool intercepted)
    {
        return await NotifyLocationChangingAsync(uri, intercepted);
    }

    /// <inheritdoc />
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(NavigationOptions))]
    protected override void NavigateToCore(string uri, NavigationOptions options)
    {
        if (uri == null)
        {
            throw new ArgumentNullException(nameof(uri));
        }

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
                DefaultWebAssemblyJSRuntime.Instance.InvokeVoid(Interop.NavigateTo, uri, options);
            }
        }, TaskScheduler.Current);
    }

    protected override void SetNavigationLockState(bool value)
        => DefaultWebAssemblyJSRuntime.Instance.InvokeVoid(Interop.SetHasLocationChangingListeners, value);

    private static partial class Log
    {
        [LoggerMessage(1, LogLevel.Debug, "Navigation canceled when changing the location to {Uri}", EventName = "NavigationCanceled")]
        public static partial void NavigationCanceled(ILogger logger, string uri);

        [LoggerMessage(2, LogLevel.Error, "Navigation failed when changing the location to {Uri}", EventName = "NavigationFailed")]
        public static partial void NavigationFailed(ILogger logger, string uri, Exception exception);
    }
}
