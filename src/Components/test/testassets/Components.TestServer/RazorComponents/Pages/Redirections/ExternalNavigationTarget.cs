// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.WebUtilities;

namespace Components.TestServer.RazorComponents.Pages.Redirections;

internal sealed class ExternalNavigationTarget
{
    public ExternalNavigationTarget(IConfiguration configuration)
    {
        Uri = new Uri(configuration["ExternalNavigationTargetUri"] ?? "https://microsoft.com");
        UriWithQuery = QueryHelpers.AddQueryString(Uri.AbsoluteUri, "foo", "🙂");
    }

    public Uri Uri { get; }

    public string UriWithQuery { get; }
}
