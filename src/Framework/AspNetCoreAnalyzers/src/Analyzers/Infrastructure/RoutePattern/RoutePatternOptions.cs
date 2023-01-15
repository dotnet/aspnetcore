// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Analyzers.Infrastructure.RoutePattern;

internal sealed class RoutePatternOptions
{
    private RoutePatternOptions() { }
    public bool SupportTokenReplacement { get; private set; }
    public bool SupportComplexSegments { get; private set; }
    public bool SupportDefaultValues { get; private set; }
    public bool SupportTwoAsteriskCatchAll { get; private set; }
    public char[]? AdditionalInvalidParameterCharacters { get; private set; }

    public static readonly RoutePatternOptions DefaultRoute = new RoutePatternOptions
    {
        SupportComplexSegments = true,
        SupportDefaultValues = true,
        SupportTwoAsteriskCatchAll = true,
    };

    public static readonly RoutePatternOptions MvcAttributeRoute = new RoutePatternOptions
    {
        SupportComplexSegments = true,
        SupportDefaultValues = true,
        SupportTwoAsteriskCatchAll = true,
        SupportTokenReplacement = true,
    };

    public static readonly RoutePatternOptions ComponentsRoute = new RoutePatternOptions
    {
        AdditionalInvalidParameterCharacters = new[] { '{', '}', '=', '.' }
    };
}
