// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Routing;

namespace Microsoft.AspNetCore.Components.WebView.Services;

internal sealed class WebViewNavigationInterception : INavigationInterception
{
    // On this platform, it's sufficient for the JS-side code to enable it unconditionally,
    // so there's no need to send a notification.
    public Task EnableNavigationInterceptionAsync() => Task.CompletedTask;
}
