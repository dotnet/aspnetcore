// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.Routing;

namespace RoutingWebSite;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class HttpMergeAttribute : Attribute, IActionHttpMethodProvider, IRouteTemplateProvider
{
    private static readonly IEnumerable<string> _supportedMethods = new[] { "MERGE" };

    public HttpMergeAttribute(string template)
    {
        Template = template;
    }

    public IEnumerable<string> HttpMethods
    {
        get { return _supportedMethods; }
    }

    /// <inheritdoc />
    public string Template { get; private set; }

    /// <inheritdoc />
    public int? Order { get; set; }

    /// <inheritdoc />
    public string Name { get; set; }
}
