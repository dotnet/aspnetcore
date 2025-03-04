// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Routing;

namespace Microsoft.AspNetCore.Components.Endpoints;

internal sealed class HttpNavigationManager : NavigationManager, IHostEnvironmentNavigationManager
{
    private readonly IRazorComponentEndpointInvoker _invoker;

    public HttpNavigationManager(IRazorComponentEndpointInvoker invoker)
    {
        _invoker = invoker;
    }

    void IHostEnvironmentNavigationManager.Initialize(string baseUri, string uri) => Initialize(baseUri, uri);

    protected override void NavigateToCore(string uri, NavigationOptions options)
    {
        var absoluteUriString = ToAbsoluteUri(uri).AbsoluteUri;
        throw new NavigationException(absoluteUriString);
    }

    protected override void NotFoundCore()
    {
        _invoker.SetNotFound();
    }
}
