// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Analyzers
{
    internal class MiddlewareAnalysis
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
}
