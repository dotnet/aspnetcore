// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Analyzers
{
    internal abstract class ConfigureServicesMethodAnalysis : StartupComputedAnalysis
    {
        protected ConfigureServicesMethodAnalysis(IMethodSymbol configureServicesMethod)
            : base(configureServicesMethod.ContainingType)
        {
            ConfigureServicesMethod = configureServicesMethod;
        }

        public IMethodSymbol ConfigureServicesMethod { get; }
    }
}
