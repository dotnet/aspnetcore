// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures;

internal sealed class HttpNavigationManager : NavigationManager, IHostEnvironmentNavigationManager
{
    void IHostEnvironmentNavigationManager.Initialize(string baseUri, string uri) => Initialize(baseUri, uri);

    protected override void NavigateToCore(string uri, bool forceLoad)
    {
        throw new NavigationException(uri);
    }
}
