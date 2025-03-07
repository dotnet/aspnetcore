// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Routing;

namespace Microsoft.AspNetCore.Components.Endpoints;

internal sealed class HttpNavigationManager : NavigationManager, IHostEnvironmentNavigationManager
{
    private IEndpointHtmlRenderer? _renderer;

    void IHostEnvironmentNavigationManager.Initialize(string baseUri, string uri) => Initialize(baseUri, uri);

    void IHostEnvironmentNavigationManager.Initialize(string baseUri, string uri, IEndpointHtmlRenderer? renderer)
    {
        Initialize(baseUri, uri);
        _renderer = renderer;
    }

    protected override void NavigateToCore(string uri, NavigationOptions options)
    {
        var absoluteUriString = ToAbsoluteUri(uri).AbsoluteUri;
        throw new NavigationException(absoluteUriString);
    }

    protected override void NotFoundCore()
    {
        _renderer?.SetNotFoundResponse();
    }
}
