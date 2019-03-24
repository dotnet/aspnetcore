// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Analyzers
{
    internal abstract class ConfigureMethodAnalysis : StartupComputedAnalysis
    {
        protected ConfigureMethodAnalysis(IMethodSymbol configureMethod)
            : base(configureMethod.ContainingType)
        {
            ConfigureMethod = configureMethod;
        }

        public IMethodSymbol ConfigureMethod { get; }
    }
}
