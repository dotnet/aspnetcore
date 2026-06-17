// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Analyzers.Infrastructure.RoutePattern;

internal sealed class RoutePatternOptions
{
    private RoutePatternOptions() { }
    public bool SupportTokenReplacement { get; private set; }

    public static readonly RoutePatternOptions DefaultRoute = new RoutePatternOptions();
    public static readonly RoutePatternOptions ComponentsRoute = new RoutePatternOptions();

    public static readonly RoutePatternOptions MvcAttributeRoute = new RoutePatternOptions
    {
        SupportTokenReplacement = true,
    };
}
