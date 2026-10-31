// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.Routing;

namespace VersioningWebSite;

public class VersionDeleteAttribute : VersionRouteAttribute, IActionHttpMethodProvider
{
    public VersionDeleteAttribute(string template)
        : base(template)
    {
    }

    public VersionDeleteAttribute(string template, string versionRange)
        : base(template, versionRange)
    {
    }

    private readonly IEnumerable<string> _httpMethods = new[] { "DELETE" };

    public IEnumerable<string> HttpMethods
    {
        get
        {
            return _httpMethods;
        }
    }
}
