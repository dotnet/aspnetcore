// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Analyzers
{
    internal static class StartupFacts
    {
        public static bool IsStartupClass(StartupSymbols symbols, INamedTypeSymbol type)
        {
            if (symbols == null)
            {
                throw new ArgumentNullException(nameof(symbols));
            }

            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            // It's not good enough to just look for ConfigureServices or Configure as a hueristic. 
            // ConfigureServices might not appear in trivial cases, and Configure might be named ConfigureDevelopment
            // or something similar. 
            //
            // Since we already are analyzing the symbol it should be cheap to do a quick pass over the members.
            var members = type.GetMembers();
            for (var i = 0; i < members.Length; i++)
            {
                if (members[i] is IMethodSymbol method && (IsConfigureServices(symbols, method) || IsConfigure(symbols, method)))
                {
                    return true;
                }
            }

            return false;
        }

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

            if (!string.Equals(symbol.Name, "ConfigureServices", StringComparison.Ordinal))
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
                if (symbol.Parameters[i].Type == symbols.IApplicationBuilder)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
