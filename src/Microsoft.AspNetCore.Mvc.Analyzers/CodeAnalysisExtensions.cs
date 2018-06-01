// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Mvc.Analyzers
{
    internal static class CodeAnalysisExtensions
    {
        public static bool HasAttribute(this ITypeSymbol typeSymbol, ITypeSymbol attribute, bool inherit)
        {
            foreach (var type in typeSymbol.GetTypeHierarchy())
            {
                if (type.HasAttribute(attribute))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool HasAttribute(this ISymbol symbol, ITypeSymbol attribute)
        {
            Debug.Assert(symbol != null);
            Debug.Assert(attribute != null);

            foreach (var declaredAttribute in symbol.GetAttributes())
            {
                if (declaredAttribute.AttributeClass == attribute)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsAssignableFrom(this ITypeSymbol source, INamedTypeSymbol target)
        {
            Debug.Assert(source != null);
            Debug.Assert(target != null);

            if (source.TypeKind == TypeKind.Interface)
            {
                foreach (var @interface in target.AllInterfaces)
                {
                    if (source == @interface)
                    {
                        return true;
                    }
                }

                return false;
            }

            foreach (var type in target.GetTypeHierarchy())
            {
                if (source == type)
                {
                    return true;
                }
            }

            return false;
        }

        private static IEnumerable<ITypeSymbol> GetTypeHierarchy(this ITypeSymbol typeSymbol)
        {
            while (typeSymbol != null)
            {
                yield return typeSymbol;

                typeSymbol = typeSymbol.BaseType;
            }
        }
    }
}
