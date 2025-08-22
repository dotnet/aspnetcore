// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;

public class NavLinkNotIgnoreQueryOrFragmentString : NavLink
{
    string hrefAbsolute;
    NavigationManager _navigationManager;

    public NavLinkNotIgnoreQueryOrFragmentString(NavigationManager navigationManager)
    {
        _navigationManager = navigationManager;
    }

    protected override void OnInitialized()
    {
        string href = "";
        if (AdditionalAttributes != null && AdditionalAttributes.TryGetValue("href", out var obj))
        {
            href = Convert.ToString(obj, CultureInfo.InvariantCulture) ?? "";
        }
        hrefAbsolute = _navigationManager.ToAbsoluteUri(href).AbsoluteUri;
        base.OnInitialized();
    }
    protected override bool ShouldMatch(string currentUriAbsolute)
    {
        bool baseMatch = base.ShouldMatch(currentUriAbsolute);
        if (!baseMatch || string.IsNullOrEmpty(hrefAbsolute) || Match != NavLinkMatch.All)
        {
            return baseMatch;
        }

        if (NormalizeUri(hrefAbsolute) == NormalizeUri(currentUriAbsolute))
        {
            return true;
        }
        return false;
    }

    private static string NormalizeUri(string uri) =>
        uri.EndsWith('/') ? uri.TrimEnd('/') : uri;
}
