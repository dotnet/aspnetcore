// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Routing;

namespace Microsoft.AspNetCore.Components.WebAssembly.Services;

internal sealed class WebAssemblyScrollToLocationHash : IScrollToLocationHash
{
    public static readonly WebAssemblyScrollToLocationHash Instance = new WebAssemblyScrollToLocationHash();

    public Task RefreshScrollPositionForHash(string locationAbsolute)
    {
        var hashIndex = locationAbsolute.IndexOf("#", StringComparison.Ordinal);

        if (hashIndex > -1 && locationAbsolute.Length > hashIndex + 1)
        {
            var elementId = locationAbsolute[(hashIndex + 1)..];

            InternalJSImportMethods.Instance.NavigationManager_ScrollToElement(elementId);
        }

        return Task.CompletedTask;
    }
}
