// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.WebView.Services;

internal sealed class WebViewNavigationManager : NavigationManager
{
    private IpcSender _ipcSender;

    public void AttachToWebView(IpcSender ipcSender, string baseUrl, string initialUrl)
    {
        _ipcSender = ipcSender;
        Initialize(baseUrl, initialUrl);
    }

    public void LocationUpdated(string newUrl, string? state, bool intercepted)
    {
        Uri = newUrl;
        NotifyLocationChanged(state, intercepted);
    }

    protected override void NavigateToCore(string uri, NavigationOptions options)
    {
        _ipcSender.Navigate(uri, options);
    }
}
