// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Routing;

namespace Microsoft.AspNetCore.Components.Endpoints;

internal sealed class HttpNavigationManager : NavigationManager, IHostEnvironmentNavigationManager
{
    private const string _enableThrowNavigationException = "Microsoft.AspNetCore.Components.Endpoints.HttpNavigationManager.EnableThrowNavigationException";

    private static bool _throwNavigationException =>
        AppContext.TryGetSwitch(_enableThrowNavigationException, out var switchValue) && switchValue;

    void IHostEnvironmentNavigationManager.Initialize(string baseUri, string uri) => Initialize(baseUri, uri);

    protected override void NavigateToCore(string uri, NavigationOptions options)
    {
        var absoluteUriString = ToAbsoluteUri(uri).AbsoluteUri;
        if (_throwNavigationException)
        {
            throw new NavigationException(absoluteUriString);
        }
        else
        {
            Uri = absoluteUriString;
            NotifyLocationChanged(isInterceptedLink: false);
        }
    }
}
