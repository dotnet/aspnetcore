// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Mvc.Analyzers
{
    internal static class CodeAnalysisExtensions
    {
        public static bool HasAttribute(this ITypeSymbol typeSymbol, ITypeSymbol attribute, bool inherit)
        {
            while (typeSymbol != null)
            {
                if (typeSymbol.HasAttribute(attribute))
                {
                    return true;
                }

                typeSymbol = typeSymbol.BaseType;
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

            if (source == target)
            {
                return true;
            }

            if (target.TypeKind == TypeKind.Interface)
            {
                foreach (var @interface in source.AllInterfaces)
                {
                    if (@interface == target)
                    {
                        return true;
                    }
                }

                return false;
            }

            do
            {
                if (source == target)
                {
                    return true;
                }

                source = source.BaseType;
            } while (source != null);

            return false;
        }
    }
}
