// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Analyzers;

internal sealed class MiddlewareAnalysis
{
    public MiddlewareAnalysis(IMethodSymbol configureMethod, ImmutableArray<MiddlewareItem> middleware)
    {
        ConfigureMethod = configureMethod;
        Middleware = middleware;
    }

    public INamedTypeSymbol StartupType => ConfigureMethod.ContainingType;

    public IMethodSymbol ConfigureMethod { get; }

    public ImmutableArray<MiddlewareItem> Middleware { get; }
}
