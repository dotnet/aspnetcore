// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Mvc.Api.Analyzers;

internal static class SymbolApiResponseMetadataProvider
{
    private const string StatusCodeProperty = "StatusCode";
    private const string StatusCodeConstructorParameter = "statusCode";
    private static readonly IList<DeclaredApiResponseMetadata> DefaultResponseMetadatas = new[]
    {
            DeclaredApiResponseMetadata.ImplicitResponse,
        };

    public static IList<DeclaredApiResponseMetadata> GetDeclaredResponseMetadata(
        in ApiControllerSymbolCache symbolCache,
        IMethodSymbol method)
    {
        var metadataItems = GetResponseMetadataFromMethodAttributes(symbolCache, method);
        if (metadataItems.Count != 0)
        {
            return metadataItems;
        }

        var conventionTypeAttributes = GetConventionTypes(symbolCache, method);
        metadataItems = GetResponseMetadataFromConventions(symbolCache, method, conventionTypeAttributes);

        if (metadataItems.Count == 0)
        {
            // If no metadata can be gleaned either through explicit attributes on the method or via a convention,
            // declare an implicit 200 status code.
            metadataItems = DefaultResponseMetadatas;
        }

        return metadataItems;
    }

    public static ITypeSymbol GetErrorResponseType(
        in ApiControllerSymbolCache symbolCache,
        IMethodSymbol method)
    {
        var errorTypeAttribute =
            method.GetAttributes(symbolCache.ProducesErrorResponseTypeAttribute).FirstOrDefault() ??
            method.ContainingType.GetAttributes(symbolCache.ProducesErrorResponseTypeAttribute).FirstOrDefault() ??
            method.ContainingAssembly.GetAttributes(symbolCache.ProducesErrorResponseTypeAttribute).FirstOrDefault();

        ITypeSymbol errorType = symbolCache.ProblemDetails;
        if (errorTypeAttribute != null &&
            errorTypeAttribute.ConstructorArguments.Length == 1 &&
            errorTypeAttribute.ConstructorArguments[0].Kind == TypedConstantKind.Type &&
            errorTypeAttribute.ConstructorArguments[0].Value is ITypeSymbol typeSymbol)
        {
            errorType = typeSymbol;
        }

        return errorType;
    }

    private static IList<DeclaredApiResponseMetadata> GetResponseMetadataFromConventions(
        in ApiControllerSymbolCache symbolCache,
        IMethodSymbol method,
        IReadOnlyList<ITypeSymbol> conventionTypes)
    {
        var conventionMethod = GetMethodFromConventionMethodAttribute(symbolCache, method);
        if (conventionMethod == null)
        {
            conventionMethod = MatchConventionMethod(symbolCache, method, conventionTypes);
        }

        if (conventionMethod != null)
        {
            return GetResponseMetadataFromMethodAttributes(symbolCache, conventionMethod);
        }

        return Array.Empty<DeclaredApiResponseMetadata>();
    }

    private static IMethodSymbol? GetMethodFromConventionMethodAttribute(in ApiControllerSymbolCache symbolCache, IMethodSymbol method)
    {
        var attribute = method.GetAttributes(symbolCache.ApiConventionMethodAttribute, inherit: true)
            .FirstOrDefault();

        if (attribute == null)
        {
            return null;
        }

        if (attribute.ConstructorArguments.Length != 2)
        {
            return null;
        }

        if (attribute.ConstructorArguments[0].Kind != TypedConstantKind.Type ||
            !(attribute.ConstructorArguments[0].Value is ITypeSymbol conventionType))
        {
            return null;
        }

        if (attribute.ConstructorArguments[1].Kind != TypedConstantKind.Primitive ||
            !(attribute.ConstructorArguments[1].Value is string conventionMethodName))
        {
            return null;
        }

        var conventionMethod = conventionType.GetMembers(conventionMethodName)
            .FirstOrDefault(m => m.Kind == SymbolKind.Method && m.IsStatic && m.DeclaredAccessibility == Accessibility.Public);

        return (IMethodSymbol)conventionMethod;
    }

    private static IMethodSymbol? MatchConventionMethod(
        in ApiControllerSymbolCache symbolCache,
        IMethodSymbol method,
        IReadOnlyList<ITypeSymbol> conventionTypes)
    {
        foreach (var conventionType in conventionTypes)
        {
            foreach (var conventionMethod in conventionType.GetMembers().OfType<IMethodSymbol>())
            {
                if (!conventionMethod.IsStatic || conventionMethod.DeclaredAccessibility != Accessibility.Public)
                {
                    continue;
                }

                if (SymbolApiConventionMatcher.IsMatch(symbolCache, method, conventionMethod))
                {
                    return conventionMethod;
                }
            }
        }

        return null;
    }

    private static IList<DeclaredApiResponseMetadata> GetResponseMetadataFromMethodAttributes(in ApiControllerSymbolCache symbolCache, IMethodSymbol methodSymbol)
    {
        var metadataItems = new List<DeclaredApiResponseMetadata>();
        var responseMetadataAttributes = methodSymbol.GetAttributes(symbolCache.ProducesResponseTypeAttribute, inherit: true);
        foreach (var attribute in responseMetadataAttributes)
        {
            var statusCode = GetStatusCode(attribute);
            var metadata = DeclaredApiResponseMetadata.ForProducesResponseType(statusCode, attribute, attributeSource: methodSymbol);

            metadataItems.Add(metadata);
        }

        var producesDefaultResponse = methodSymbol.GetAttributes(symbolCache.ProducesDefaultResponseTypeAttribute, inherit: true).FirstOrDefault();
        if (producesDefaultResponse != null)
        {
            metadataItems.Add(DeclaredApiResponseMetadata.ForProducesDefaultResponse(producesDefaultResponse, methodSymbol));
        }

        return metadataItems;
    }

    internal static IReadOnlyList<ITypeSymbol> GetConventionTypes(in ApiControllerSymbolCache symbolCache, IMethodSymbol method)
    {
        var attributes = method.ContainingType.GetAttributes(symbolCache.ApiConventionTypeAttribute, inherit: true).ToArray();
        if (attributes.Length == 0)
        {
            attributes = method.ContainingAssembly.GetAttributes(symbolCache.ApiConventionTypeAttribute).ToArray();
        }

        var conventionTypes = new List<ITypeSymbol>();
        for (var i = 0; i < attributes.Length; i++)
        {
            var attribute = attributes[i];
            if (attribute.ConstructorArguments.Length != 1 ||
                attribute.ConstructorArguments[0].Kind != TypedConstantKind.Type ||
                !(attribute.ConstructorArguments[0].Value is ITypeSymbol conventionType))
            {
                continue;
            }

            conventionTypes.Add(conventionType);
        }

        return conventionTypes;
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
