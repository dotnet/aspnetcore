// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Analyzers
{
    internal abstract class StartupComputedAnalysis
    {
        protected StartupComputedAnalysis(INamedTypeSymbol enclosingType)
        {
            EnclosingType = enclosingType;
        }

        public INamedTypeSymbol EnclosingType { get; }
    }
}
