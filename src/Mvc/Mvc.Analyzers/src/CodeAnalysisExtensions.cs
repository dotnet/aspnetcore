// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.CodeAnalysis
{
    internal static class CodeAnalysisExtensions
    {
        public static bool HasAttribute(this ITypeSymbol typeSymbol, ITypeSymbol attribute, bool inherit)
            => GetAttributes(typeSymbol, attribute, inherit).Any();

        public static bool HasAttribute(this IMethodSymbol methodSymbol, ITypeSymbol attribute, bool inherit)
            => GetAttributes(methodSymbol, attribute, inherit).Any();

        public static IEnumerable<AttributeData> GetAttributes(this ISymbol symbol, ITypeSymbol attribute)
        {
            foreach (var declaredAttribute in symbol.GetAttributes())
            {
                if (attribute.IsAssignableFrom(declaredAttribute.AttributeClass))
                {
                    yield return declaredAttribute;
                }
            }
        }

        public static IEnumerable<AttributeData> GetAttributes(this IMethodSymbol methodSymbol, ITypeSymbol attribute, bool inherit)
        {
            Debug.Assert(methodSymbol != null);
            Debug.Assert(attribute != null);

            while (methodSymbol != null)
            {
                foreach (var attributeData in GetAttributes(methodSymbol, attribute))
                {
                    yield return attributeData;
                }

                if (!inherit)
                {
                    break;
                }

                methodSymbol = methodSymbol.IsOverride ? methodSymbol.OverriddenMethod : null;
            }
        }

        public static IEnumerable<AttributeData> GetAttributes(this ITypeSymbol typeSymbol, ITypeSymbol attribute, bool inherit)
        {
            Debug.Assert(typeSymbol != null);
            Debug.Assert(attribute != null);

            foreach (var type in GetTypeHierarchy(typeSymbol))
            {
                foreach (var attributeData in GetAttributes(type, attribute))
                {
                    yield return attributeData;
                }

                if (!inherit)
                {
                    break;
                }
            }
        }

        public static bool HasAttribute(this IPropertySymbol propertySymbol, ITypeSymbol attribute, bool inherit)
        {
            Debug.Assert(propertySymbol != null);
            Debug.Assert(attribute != null);

            if (!inherit)
            {
                return HasAttribute(propertySymbol, attribute);
            }

            while (propertySymbol != null)
            {
                if (propertySymbol.HasAttribute(attribute))
                {
                    return true;
                }

                propertySymbol = propertySymbol.IsOverride ? propertySymbol.OverriddenProperty : null;
            }

            return false;
        }

        public static bool IsAssignableFrom(this ITypeSymbol source, ITypeSymbol target)
        {
            Debug.Assert(source != null);
            Debug.Assert(target != null);

            if (source == target)
            {
                return true;
            }

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

        public static bool HasAttribute(this ISymbol symbol, ITypeSymbol attribute)
        {
            foreach (var declaredAttribute in symbol.GetAttributes())
            {
                if (attribute.IsAssignableFrom(declaredAttribute.AttributeClass))
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
