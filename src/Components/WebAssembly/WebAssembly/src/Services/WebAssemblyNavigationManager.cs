// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.JSInterop;
using Interop = Microsoft.AspNetCore.Components.Web.BrowserNavigationManagerInterop;

namespace Microsoft.AspNetCore.Components.WebAssembly.Services;

/// <summary>
/// Default client-side implementation of <see cref="NavigationManager"/>.
/// </summary>
internal sealed class WebAssemblyNavigationManager : NavigationManager
{
    /// <summary>
    /// Gets the instance of <see cref="WebAssemblyNavigationManager"/>.
    /// </summary>
    public static WebAssemblyNavigationManager Instance { get; set; } = default!;

    public WebAssemblyNavigationManager(string baseUri, string uri)
    {
        Initialize(baseUri, uri);
    }

    public void SetLocation(string uri, string? state, bool isInterceptedLink)
    {
        Uri = uri;
        HistoryEntryState = state;
        NotifyLocationChanged(isInterceptedLink);
    }

    public ValueTask<bool> HandleLocationChanging(string uri, bool intercepted)
    {
        return NotifyLocationChanging(uri, intercepted);
    }

    /// <inheritdoc />
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(NavigationOptions))]
    protected override async void NavigateToCore(string uri, NavigationOptions options)
    {
        if (uri == null)
        {
            throw new ArgumentNullException(nameof(uri));
        }

        var shouldCancel = await NotifyLocationChanging(uri, false);

        if (!shouldCancel)
        {
            DefaultWebAssemblyJSRuntime.Instance.InvokeVoid(Interop.NavigateTo, uri, options);
        }
    }

    protected override void SetNavigationLockState(bool value)
        => DefaultWebAssemblyJSRuntime.Instance.InvokeVoid(Interop.SetHasLocationChangingListeners, value);
}
