// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Analyzers
{
    internal class OptionsAnalysis
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
}
