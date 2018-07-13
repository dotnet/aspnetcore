// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.AspNetCore.Mvc.Analyzers
{
    internal static class SymbolApiResponseMetadataProvider
    {
        private const string StatusCodeProperty = "StatusCode";
        private const string StatusCodeConstructorParameter = "statusCode";
        private static readonly Func<SyntaxNode, bool> _shouldDescendIntoChildren = ShouldDescendIntoChildren;

        internal static IList<DeclaredApiResponseMetadata> GetDeclaredResponseMetadata(
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

        private static IList<DeclaredApiResponseMetadata> GetResponseMetadataFromConventions(
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

            return Array.Empty<DeclaredApiResponseMetadata>();
        }

        private static IList<DeclaredApiResponseMetadata> GetResponseMetadataFromMethodAttributes(ApiControllerSymbolCache symbolCache, IMethodSymbol methodSymbol)
        {
            var metadataItems = new List<DeclaredApiResponseMetadata>();
            var responseMetadataAttributes = methodSymbol.GetAttributes(symbolCache.ProducesResponseTypeAttribute, inherit: true);
            foreach (var attribute in responseMetadataAttributes)
            {
                var statusCode = GetStatusCode(attribute);
                var metadata = new DeclaredApiResponseMetadata(statusCode, attribute, convention: null);

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

        internal static bool TryGetActualResponseMetadata(
            in ApiControllerSymbolCache symbolCache,
            SemanticModel semanticModel,
            MethodDeclarationSyntax methodSyntax,
            CancellationToken cancellationToken,
            out IList<ActualApiResponseMetadata> actualResponseMetadata)
        {
            actualResponseMetadata = new List<ActualApiResponseMetadata>();

            var hasUnreadableReturnStatements = false;

            foreach (var returnStatementSyntax in methodSyntax.DescendantNodes(_shouldDescendIntoChildren).OfType<ReturnStatementSyntax>())
            {
                var responseMetadata = InspectReturnStatementSyntax(
                    symbolCache,
                    semanticModel,
                    returnStatementSyntax,
                    cancellationToken);

                if (responseMetadata != null)
                {
                    actualResponseMetadata.Add(responseMetadata.Value);
                }
                else
                {
                    hasUnreadableReturnStatements = true;
                }
            }

            return hasUnreadableReturnStatements;
        }

        internal static ActualApiResponseMetadata? InspectReturnStatementSyntax(
            in ApiControllerSymbolCache symbolCache,
            SemanticModel semanticModel,
            ReturnStatementSyntax returnStatementSyntax,
            CancellationToken cancellationToken)
        {
            var returnExpression = returnStatementSyntax.Expression;
            if (returnExpression.IsMissing)
            {
                return null;
            }

            var typeInfo = semanticModel.GetTypeInfo(returnExpression, cancellationToken);
            if (typeInfo.Type.TypeKind == TypeKind.Error)
            {
                return null;
            }

            var statementReturnType = typeInfo.Type;

            var defaultStatusCodeAttribute = statementReturnType
                .GetAttributes(symbolCache.DefaultStatusCodeAttribute, inherit: true)
                .FirstOrDefault();

            if (defaultStatusCodeAttribute != null)
            {
                var statusCode = GetDefaultStatusCode(defaultStatusCodeAttribute);
                if (statusCode == null)
                {
                    // Unable to read the status code even though the attribute exists.
                    return null;
                }

                return new ActualApiResponseMetadata(returnStatementSyntax, statusCode.Value);
            }
            else if (!symbolCache.IActionResult.IsAssignableFrom(statementReturnType))
            {
                // Return expression does not have a DefaultStatusCodeAttribute and it is not
                // an instance of IActionResult. Must be returning the "model".
                return new ActualApiResponseMetadata(returnStatementSyntax);
            }

            return null;
        }

        private static bool ShouldDescendIntoChildren(SyntaxNode syntaxNode)
        {
            return !syntaxNode.IsKind(SyntaxKind.LocalFunctionStatement) &&
                !syntaxNode.IsKind(SyntaxKind.ParenthesizedLambdaExpression) &&
                !syntaxNode.IsKind(SyntaxKind.SimpleLambdaExpression) &&
                !syntaxNode.IsKind(SyntaxKind.AnonymousMethodExpression);
        }

        internal static int? GetDefaultStatusCode(AttributeData attribute)
        {
            if (attribute != null &&
                attribute.ConstructorArguments.Length == 1 &&
                attribute.ConstructorArguments[0].Kind == TypedConstantKind.Primitive &&
                attribute.ConstructorArguments[0].Value is int statusCode)
            {
                return statusCode;
            }

            return null;
        }
    }
}