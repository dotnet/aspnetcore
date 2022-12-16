// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Analyzers;

internal sealed class OptionsAnalysis
{
    public OptionsAnalysis(IMethodSymbol configureServicesMethod, ImmutableArray<OptionsItem> options)
    {
        ConfigureServicesMethod = configureServicesMethod;
        Options = options;
    }

    public INamedTypeSymbol StartupType => ConfigureServicesMethod.ContainingType;

    public IMethodSymbol ConfigureServicesMethod { get; }

    public ImmutableArray<OptionsItem> Options { get; }
}
