// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.Razor
{
    internal static class ComponentDetectionConventions
    {
        public static bool IsComponent(INamedTypeSymbol symbol, INamedTypeSymbol icomponentSymbol)
        {
            if (symbol is null)
            {
                throw new ArgumentNullException(nameof(symbol));
            }

            if (icomponentSymbol is null)
            {
                throw new ArgumentNullException(nameof(icomponentSymbol));
            }

            return
                //symbol.DeclaredAccessibility == Accessibility.Public && // we do not need check accessibility anymore
                !symbol.IsAbstract &&
                symbol.AllInterfaces.Contains(icomponentSymbol);
        }
    }
}
