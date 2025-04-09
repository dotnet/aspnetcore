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
            if (!IsInternalUri(absoluteUriString))
            {
                // it's an external navigation, avoid Uri validation exception
                BaseUri = GetBaseUriFromAbsoluteUri(absoluteUriString);
            }
            Uri = absoluteUriString;
            NotifyLocationChanged(isInterceptedLink: false);
        }
    }

    // ToDo: the following are copy-paste, consider refactoring to a common place
    private bool IsInternalUri(string uri)
    {
        var normalizedBaseUri = NormalizeBaseUri(BaseUri);
        return uri.StartsWith(normalizedBaseUri, StringComparison.OrdinalIgnoreCase);
    }

    private static string GetBaseUriFromAbsoluteUri(string absoluteUri)
    {
        // Find the position of the first single slash after the scheme (e.g., "https://")
        var schemeDelimiterIndex = absoluteUri.IndexOf("://", StringComparison.Ordinal);
        if (schemeDelimiterIndex == -1)
        {
            throw new ArgumentException($"The provided URI '{absoluteUri}' is not a valid absolute URI.");
        }

        // Find the end of the authority section (e.g., "https://example.com/")
        var authorityEndIndex = absoluteUri.IndexOf('/', schemeDelimiterIndex + 3);
        if (authorityEndIndex == -1)
        {
            // If no slash is found, the entire URI is the authority (e.g., "https://example.com")
            return NormalizeBaseUri(absoluteUri + "/");
        }

        // Extract the base URI up to the authority section
        return NormalizeBaseUri(absoluteUri.Substring(0, authorityEndIndex + 1));
    }

    private static string NormalizeBaseUri(string baseUri)
    {
        var lastSlashIndex = baseUri.LastIndexOf('/');
        if (lastSlashIndex >= 0)
        {
            baseUri = baseUri.Substring(0, lastSlashIndex + 1);
        }

        return baseUri;
    }
}
