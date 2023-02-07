// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.AspNetCore.Http.Generators.StaticRouteHandlerModel;

public class EndpointRoute
{
    private const int RoutePatternArgumentOrdinal = 1;

    public string RoutePattern { get; init; }

    public List<DiagnosticDescriptor> Diagnostics { get; init; } = new List<DiagnosticDescriptor>();

    public EndpointRoute(IInvocationOperation operation)
    {
        if (!TryGetRouteHandlerPattern(operation, out var routeToken))
        {
            Diagnostics.Add(DiagnosticDescriptors.UnableToResolveRoutePattern);
        }

        RoutePattern = routeToken.ValueText;
    }

    private static bool TryGetRouteHandlerPattern(IInvocationOperation invocation, out SyntaxToken token)
    {
        IArgumentOperation? argumentOperation = null;
        foreach (var argument in invocation.Arguments)
        {
            if (argument.Parameter?.Ordinal == RoutePatternArgumentOrdinal)
            {
                argumentOperation = argument;
            }
        }
        if (argumentOperation?.Syntax is not ArgumentSyntax routePatternArgumentSyntax ||
            routePatternArgumentSyntax.Expression is not LiteralExpressionSyntax routePatternArgumentLiteralSyntax)
        {
            token = default;
            return false;
        }
        token = routePatternArgumentLiteralSyntax.Token;
        return true;
    }
}
