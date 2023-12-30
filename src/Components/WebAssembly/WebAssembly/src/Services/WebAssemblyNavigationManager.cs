// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components.Routing;
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

    public async ValueTask<bool> HandleLocationChangingAsync(string uri, string? state, bool intercepted)
    {
        return await NotifyLocationChangingAsync(uri, state, intercepted);
    }

    /// <inheritdoc />
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(NavigationOptions))]
    protected override void NavigateToCore(string uri, NavigationOptions options)
    {
        ArgumentNullException.ThrowIfNull(uri);

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

                DefaultWebAssemblyJSRuntime.Instance.InvokeVoid(Interop.NavigateTo, uri, options);
            }
            catch (Exception ex)
            {
                // We shouldn't ever reach this since exceptions thrown from handlers are handled in HandleLocationChangingHandlerException.
                // But if some other exception gets thrown, we still want to know about it.
                Log.NavigationFailed(_logger, uri, ex);
            }
        }
    }

    /// <inheritdoc />
    public override void Refresh(bool forceReload = false)
    {
        DefaultWebAssemblyJSRuntime.Instance.InvokeVoid(Interop.Refresh, forceReload);
    }

    protected override void HandleLocationChangingHandlerException(Exception ex, LocationChangingContext context)
    {
        Log.NavigationFailed(_logger, context.TargetLocation, ex);
    }

    protected override void SetNavigationLockState(bool value)
        => InternalJSImportMethods.Instance.NavigationManager_SetHasLocationChangingListeners((int)WebRendererId.WebAssembly, value);

    private static partial class Log
    {
        [LoggerMessage(1, LogLevel.Debug, "Navigation canceled when changing the location to {Uri}", EventName = "NavigationCanceled")]
        public static partial void NavigationCanceled(ILogger logger, string uri);

        [LoggerMessage(2, LogLevel.Error, "Navigation failed when changing the location to {Uri}", EventName = "NavigationFailed")]
        public static partial void NavigationFailed(ILogger logger, string uri, Exception exception);
    }
}
