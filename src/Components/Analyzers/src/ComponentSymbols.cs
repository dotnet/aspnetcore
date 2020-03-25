// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Components.Analyzers
{
    internal class ComponentSymbols
    {
        public static bool TryCreate(Compilation compilation, out ComponentSymbols symbols)
        {
            if (compilation == null)
            {
                throw new ArgumentNullException(nameof(compilation));
            }

            var parameterAttribute = compilation.GetTypeByMetadataName(ComponentsApi.ParameterAttribute.MetadataName);
            if (parameterAttribute == null)
            {
                symbols = null;
                return false;
            }

            var cascadingParameterAttribute = compilation.GetTypeByMetadataName(ComponentsApi.CascadingParameterAttribute.MetadataName);
            if (cascadingParameterAttribute == null)
            {
                symbols = null;
                return false;
            }

            var icomponentType = compilation.GetTypeByMetadataName(ComponentsApi.IComponent.MetadataName);
            if (icomponentType == null)
            {
                symbols = null;
                return false;
            }

            var dictionary = compilation.GetTypeByMetadataName("System.Collections.Generic.Dictionary`2");
            var @string = compilation.GetSpecialType(SpecialType.System_String);
            var @object = compilation.GetSpecialType(SpecialType.System_Object);
            if (dictionary == null || @string == null || @object == null)
            {
                symbols = null;
                return false;
            }

            var parameterCaptureUnmatchedValuesRuntimeType = dictionary.Construct(@string, @object);

            symbols = new ComponentSymbols(
                parameterAttribute,
                cascadingParameterAttribute,
                parameterCaptureUnmatchedValuesRuntimeType,
                icomponentType);
            return true;
        }

        private ComponentSymbols(
            INamedTypeSymbol parameterAttribute,
            INamedTypeSymbol cascadingParameterAttribute,
            INamedTypeSymbol parameterCaptureUnmatchedValuesRuntimeType,
            INamedTypeSymbol icomponentType)
        {
            ParameterAttribute = parameterAttribute;
            CascadingParameterAttribute = cascadingParameterAttribute;
            ParameterCaptureUnmatchedValuesRuntimeType = parameterCaptureUnmatchedValuesRuntimeType;
            IComponentType = icomponentType;
        }

        public INamedTypeSymbol ParameterAttribute { get; }

        // Dictionary<string, object>
        public INamedTypeSymbol ParameterCaptureUnmatchedValuesRuntimeType { get; }

        public INamedTypeSymbol CascadingParameterAttribute { get; }

        public INamedTypeSymbol IComponentType { get; }
    }
}
