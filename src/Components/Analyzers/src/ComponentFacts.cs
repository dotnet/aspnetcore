// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Microsoft.AspNetCore.Components.Analyzers
{
    internal static class ComponentFacts
    {
        public static bool IsAnyParameter(ComponentSymbols symbols, IPropertySymbol property)
        {
            if (symbols == null)
            {
                throw new ArgumentNullException(nameof(symbols));
            }

            if (property == null)
            {
                throw new ArgumentNullException(nameof(property));
            }

            return property.GetAttributes().Any(a =>
            {
                return a.AttributeClass == symbols.ParameterAttribute || a.AttributeClass == symbols.CascadingParameterAttribute;
            });
        }

        public static bool IsParameter(ComponentSymbols symbols, IPropertySymbol property)
        {
            if (symbols == null)
            {
                throw new ArgumentNullException(nameof(symbols));
            }

            if (property == null)
            {
                throw new ArgumentNullException(nameof(property));
            }

            return property.GetAttributes().Any(a => a.AttributeClass == symbols.ParameterAttribute);
        }

        public static bool IsParameterWithCaptureUnmatchedValues(ComponentSymbols symbols, IPropertySymbol property)
        {
            if (symbols == null)
            {
                throw new ArgumentNullException(nameof(symbols));
            }

            if (property == null)
            {
                throw new ArgumentNullException(nameof(property));
            }

            var attribute = property.GetAttributes().FirstOrDefault(a => a.AttributeClass == symbols.ParameterAttribute);
            if (attribute == null)
            {
                return false;
            }

            foreach (var kvp in attribute.NamedArguments)
            {
                if (string.Equals(kvp.Key, ComponentsApi.ParameterAttribute.CaptureUnmatchedValues, StringComparison.Ordinal))
                {
                    return kvp.Value.Value as bool? ?? false;
                }
            }

            return false;
        }

        public static bool IsCascadingParameter(ComponentSymbols symbols, IPropertySymbol property)
        {
            if (symbols == null)
            {
                throw new ArgumentNullException(nameof(symbols));
            }

            if (property == null)
            {
                throw new ArgumentNullException(nameof(property));
            }

            return property.GetAttributes().Any(a => a.AttributeClass == symbols.CascadingParameterAttribute);
        }

        public static bool IsComponent(ComponentSymbols symbols, Compilation compilation, INamedTypeSymbol type)
        {
            if (symbols is null)
            {
                throw new ArgumentNullException(nameof(symbols));
            }

            if (type is null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            var conversion = compilation.ClassifyConversion(symbols.IComponentType, type);
            if (!conversion.Exists || !conversion.IsExplicit)
            {
                return false;
            }

            return true;
        }
    }
}
