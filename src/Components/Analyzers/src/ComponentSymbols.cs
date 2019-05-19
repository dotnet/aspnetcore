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

            var dictionary = compilation.GetTypeByMetadataName(ComponentsApi.SystemCollectionsGenericDictionary);
            if (dictionary == null)
            {
                symbols = null;
                return false;
            }

            var parameterCaptureExtraAttributesValueType = dictionary.Construct(
                compilation.GetSpecialType(SpecialType.System_String),
                compilation.GetSpecialType(SpecialType.System_Object));

            symbols = new ComponentSymbols(parameterAttribute, cascadingParameterAttribute, parameterCaptureExtraAttributesValueType);
            return true;
        }

        private ComponentSymbols(
            INamedTypeSymbol parameterAttribute,
            INamedTypeSymbol cascadingParameterAttribute,
            INamedTypeSymbol parameterCaptureExtraAttributesValueType)
        {
            ParameterAttribute = parameterAttribute;
            CascadingParameterAttribute = cascadingParameterAttribute;
            ParameterCaptureExtraAttributesValueType = parameterCaptureExtraAttributesValueType;
        }

        public INamedTypeSymbol ParameterAttribute { get; }

        // Dictionary<string, object>
        public INamedTypeSymbol ParameterCaptureExtraAttributesValueType { get; }

        public INamedTypeSymbol CascadingParameterAttribute { get; }
    }
}
