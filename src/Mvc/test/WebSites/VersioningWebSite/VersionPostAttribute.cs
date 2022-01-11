// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.Routing;

namespace VersioningWebSite;

public class VersionPostAttribute : VersionRouteAttribute, IActionHttpMethodProvider
{
    public VersionPostAttribute(string template)
        : base(template)
    {
    }

    public VersionPostAttribute(string template, string versionRange)
        : base(template, versionRange)
    {
    }

    private readonly IEnumerable<string> _httpMethods = new[] { "POST" };

    public IEnumerable<string> HttpMethods
    {
        get
        {
            return _httpMethods;
        }
    }
}
