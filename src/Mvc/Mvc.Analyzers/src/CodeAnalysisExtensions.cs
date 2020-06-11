// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
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
            attribute = attribute ?? throw new ArgumentNullException(nameof(attribute));

            IMethodSymbol? current = methodSymbol;
            while (current != null)
            {
                foreach (var attributeData in GetAttributes(current, attribute))
                {
                    yield return attributeData;
                }

                if (!inherit)
                {
                    break;
                }

                current = current.IsOverride ? current.OverriddenMethod : null;
            }
        }

        public static IEnumerable<AttributeData> GetAttributes(this ITypeSymbol typeSymbol, ITypeSymbol attribute, bool inherit)
        {
            typeSymbol = typeSymbol ?? throw new ArgumentNullException(nameof(typeSymbol));
            attribute = attribute ?? throw new ArgumentNullException(nameof(attribute));

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
            propertySymbol = propertySymbol ?? throw new ArgumentNullException(nameof(propertySymbol));
            attribute = attribute ?? throw new ArgumentNullException(nameof(attribute));

            if (!inherit)
            {
                return HasAttribute(propertySymbol, attribute);
            }

            IPropertySymbol? current = propertySymbol;
            while (current != null)
            {
                if (current.HasAttribute(attribute))
                {
                    return true;
                }

                current = current.IsOverride ? current.OverriddenProperty : null;
            }

            return false;
        }

        public static bool IsAssignableFrom(this ITypeSymbol source, ITypeSymbol target)
        {
            source = source ?? throw new ArgumentNullException(nameof(source));
            target = target ?? throw new ArgumentNullException(nameof(target));

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
