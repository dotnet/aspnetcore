// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Analyzers
{
    internal static class StartupFacts
    {
        public static bool IsConfigureServices(StartupSymbols symbols, IMethodSymbol symbol)
        {
            if (symbol == null)
            {
                throw new ArgumentNullException(nameof(symbol));
            }

            if (symbol.DeclaredAccessibility != Accessibility.Public)
            {
                return false;
            }

            if (!string.Equals(symbol.Name, "ConfigureServices",StringComparison.Ordinal))
            {
                return false;
            }

            if (symbol.Parameters.Length != 1)
            {
                return false;
            }

            if (symbol.Parameters[0].Type != symbols.IServiceCollection)
            {
                return false;
            }

            return true;
        }

        public static bool IsConfigure(StartupSymbols symbols, IMethodSymbol symbol)
        {
            if (symbol == null)
            {
                throw new ArgumentNullException(nameof(symbol));
            }

            if (symbol.DeclaredAccessibility != Accessibility.Public)
            {
                return false;
            }

            if (symbol.Name == null || !symbol.Name.StartsWith("Configure", StringComparison.Ordinal))
            {
                return false;
            }

            // IApplicationBuilder can appear in any parameter
            for (var i = 0; i < symbol.Parameters.Length; i++)
            {
                if  (symbol.Parameters[i].Type == symbols.IApplicationBuilder)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
