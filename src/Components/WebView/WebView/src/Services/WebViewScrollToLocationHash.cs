// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Routing;

namespace Microsoft.AspNetCore.Components.WebView.Services;

internal sealed class WebViewScrollToLocationHash : IScrollToLocationHash
{
    public Task ScrollToLocationHash(string locationAbsolute) => Task.CompletedTask;
}
