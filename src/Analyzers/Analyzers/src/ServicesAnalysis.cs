// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Analyzers;

internal sealed class ServicesAnalysis
{
    public ServicesAnalysis(IMethodSymbol configureServicesMethod, ImmutableArray<ServicesItem> services)
    {
        ConfigureServicesMethod = configureServicesMethod;
        Services = services;
    }

    public INamedTypeSymbol StartupType => ConfigureServicesMethod.ContainingType;

    public IMethodSymbol ConfigureServicesMethod { get; }

    public ImmutableArray<ServicesItem> Services { get; }
}
