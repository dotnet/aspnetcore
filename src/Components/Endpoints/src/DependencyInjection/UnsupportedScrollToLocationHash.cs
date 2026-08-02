// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Routing;

namespace Microsoft.AspNetCore.Components.Endpoints;

internal sealed class UnsupportedScrollToLocationHash : IScrollToLocationHash
{
    public Task RefreshScrollPositionForHash(string locationAbsolute)
    {
        throw new InvalidOperationException("Scroll to location hash calls cannot be issued during server-side static rendering, because the page has not yet loaded in the browser.");
    }
}
