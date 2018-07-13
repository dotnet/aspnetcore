// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Mvc.Analyzers
{
    internal class SymbolApiResponseMetadataProvider
    {
        private const string StatusCodeProperty = "StatusCode";
        private const string StatusCodeConstructorParameter = "statusCode";

        internal static IList<ApiResponseMetadata> GetResponseMetadata(
            ApiControllerSymbolCache symbolCache,
            IMethodSymbol method,
            IReadOnlyList<AttributeData> conventionTypeAttributes)
        {
            var metadataItems = GetResponseMetadataFromMethodAttributes(symbolCache, method);
            if (metadataItems.Count != 0)
            {
                return metadataItems;
            }

            metadataItems = GetResponseMetadataFromConventions(symbolCache, method, conventionTypeAttributes);
            return metadataItems;
        }

        private static IList<ApiResponseMetadata> GetResponseMetadataFromConventions(
            ApiControllerSymbolCache symbolCache,
            IMethodSymbol method,
            IReadOnlyList<AttributeData> attributes)
        {
            foreach (var attribute in attributes)
            {
                if (attribute.ConstructorArguments.Length != 1 ||
                    attribute.ConstructorArguments[0].Kind != TypedConstantKind.Type ||
                    !(attribute.ConstructorArguments[0].Value is ITypeSymbol conventionType))
                {
                    continue;
                }

                foreach (var conventionMethod in conventionType.GetMembers().OfType<IMethodSymbol>())
                {
                    if (!conventionMethod.IsStatic || conventionMethod.DeclaredAccessibility != Accessibility.Public)
                    {
                        continue;
                    }

                    if (!SymbolApiConventionMatcher.IsMatch(symbolCache, method, conventionMethod))
                    {
                        continue;
                    }

                    return GetResponseMetadataFromMethodAttributes(symbolCache, conventionMethod);
                }
            }

            return Array.Empty<ApiResponseMetadata>();
        }

        private static IList<ApiResponseMetadata> GetResponseMetadataFromMethodAttributes(ApiControllerSymbolCache symbolCache, IMethodSymbol methodSymbol)
        {
            var metadataItems = new List<ApiResponseMetadata>();
            var responseMetadataAttributes = methodSymbol.GetAttributes(symbolCache.ProducesResponseTypeAttribute, inherit: true);
            foreach (var attribute in responseMetadataAttributes)
            {
                var statusCode = GetStatusCode(attribute);
                var metadata = new ApiResponseMetadata(statusCode, attribute, convention: null);

                metadataItems.Add(metadata);
            }

            return metadataItems;
        }

        internal static int GetStatusCode(AttributeData attribute)
        {
            const int DefaultStatusCode = 200;
            for (var i = 0; i < attribute.NamedArguments.Length; i++)
            {
                var namedArgument = attribute.NamedArguments[i];
                var namedArgumentValue = namedArgument.Value;
                if (string.Equals(namedArgument.Key, StatusCodeProperty, StringComparison.Ordinal) &&
                    namedArgumentValue.Kind == TypedConstantKind.Primitive &&
                    (namedArgumentValue.Type.SpecialType & SpecialType.System_Int32) == SpecialType.System_Int32 &&
                    namedArgumentValue.Value is int statusCode)
                {
                    return statusCode;
                }
            }

            if (attribute.AttributeConstructor == null)
            {
                return DefaultStatusCode;
            }

            var constructorParameters = attribute.AttributeConstructor.Parameters;
            for (var i = 0; i < constructorParameters.Length; i++)
            {
                var parameter = constructorParameters[i];
                if (string.Equals(parameter.Name, StatusCodeConstructorParameter, StringComparison.Ordinal) &&
                    (parameter.Type.SpecialType & SpecialType.System_Int32) == SpecialType.System_Int32)
                {
                    if (attribute.ConstructorArguments.Length < i)
                    {
                        return DefaultStatusCode;
                    }

                    var argument = attribute.ConstructorArguments[i];
                    if (argument.Kind == TypedConstantKind.Primitive && argument.Value is int statusCode)
                    {
                        return statusCode;
                    }
                }
            }

            return DefaultStatusCode;
        }
    }
}