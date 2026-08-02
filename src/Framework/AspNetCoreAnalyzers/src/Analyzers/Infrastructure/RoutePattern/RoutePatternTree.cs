// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using Microsoft.AspNetCore.Analyzers.Infrastructure.EmbeddedSyntax;
using Microsoft.AspNetCore.Analyzers.Infrastructure.VirtualChars;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.AspNetCore.Analyzers.Infrastructure.RoutePattern;

internal sealed class RoutePatternTree : EmbeddedSyntaxTree<RoutePatternKind, RoutePatternNode, RoutePatternCompilationUnit>
{
    public readonly ImmutableArray<RouteParameter> RouteParameters;

    public RoutePatternTree(
        VirtualCharSequence text,
        RoutePatternCompilationUnit root,
        ImmutableArray<EmbeddedDiagnostic> diagnostics,
        ImmutableArray<RouteParameter> routeParameters)
        : base(text, root, diagnostics)
    {
        RouteParameters = routeParameters;
    }

    public RouteParameter GetRouteParameter(string name)
    {
        if (TryGetRouteParameter(name, out var routeParameter))
        {
            return routeParameter;
        }

        throw new InvalidOperationException($"Couldn't find route parameter '{name}'.");
    }

    public bool TryGetRouteParameter(string name, out RouteParameter routeParameter)
    {
        foreach (var parameter in RouteParameters)
        {
            if (string.Equals(parameter.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                routeParameter = parameter;
                return true;
            }
        }

        routeParameter = default;
        return false;
    }
}

internal readonly struct RouteParameter
{
    public RouteParameter(string name, bool encodeSlashes, string? defaultValue, bool isOptional, bool isCatchAll, ImmutableArray<string> policies, TextSpan span)
    {
        Name = name;
        EncodeSlashes = encodeSlashes;
        DefaultValue = defaultValue;
        IsOptional = isOptional;
        IsCatchAll = isCatchAll;
        Policies = policies;
        Span = span;
    }

    public readonly string Name;
    public readonly bool EncodeSlashes;
    public readonly string? DefaultValue;
    public readonly bool IsOptional;
    public readonly bool IsCatchAll;
    public readonly ImmutableArray<string> Policies;
    public readonly TextSpan Span;

    public override string ToString()
    {
        return Name;
    }
}
