// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.AspNetCore.Mvc.Api.Analyzers
{
    internal static class SymbolApiResponseMetadataProvider
    {
        private const string StatusCodeProperty = "StatusCode";
        private const string StatusCodeConstructorParameter = "statusCode";
        private static readonly Func<SyntaxNode, bool> _shouldDescendIntoChildren = ShouldDescendIntoChildren;
        private static readonly IList<DeclaredApiResponseMetadata> DefaultResponseMetadatas = new[]
        {
            DeclaredApiResponseMetadata.ImplicitResponse,
        };

        internal static IList<DeclaredApiResponseMetadata> GetDeclaredResponseMetadata(
            ApiControllerSymbolCache symbolCache,
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

        private static IList<DeclaredApiResponseMetadata> GetResponseMetadataFromConventions(
            ApiControllerSymbolCache symbolCache,
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

        private static IMethodSymbol GetMethodFromConventionMethodAttribute(ApiControllerSymbolCache symbolCache, IMethodSymbol method)
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

        private static IMethodSymbol MatchConventionMethod(
            ApiControllerSymbolCache symbolCache,
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

        private static IList<DeclaredApiResponseMetadata> GetResponseMetadataFromMethodAttributes(ApiControllerSymbolCache symbolCache, IMethodSymbol methodSymbol)
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

        internal static IReadOnlyList<ITypeSymbol> GetConventionTypes(ApiControllerSymbolCache symbolCache, IMethodSymbol method)
        {
            var attributes = method.ContainingType.GetAttributes(symbolCache.ApiConventionTypeAttribute).ToArray();
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

        internal static bool TryGetActualResponseMetadata(
            in ApiControllerSymbolCache symbolCache,
            SemanticModel semanticModel,
            MethodDeclarationSyntax methodSyntax,
            CancellationToken cancellationToken,
            out IList<ActualApiResponseMetadata> actualResponseMetadata)
        {
            actualResponseMetadata = new List<ActualApiResponseMetadata>();

            var allReturnStatementsReadable = true;

            foreach (var returnStatementSyntax in methodSyntax.DescendantNodes(_shouldDescendIntoChildren).OfType<ReturnStatementSyntax>())
            {
                if (returnStatementSyntax.IsMissing || returnStatementSyntax.Expression.IsMissing)
                {
                    // Ignore malformed return statements.
                    continue;
                }

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
                    allReturnStatementsReadable = false;
                }
            }

            return allReturnStatementsReadable;
        }

        internal static ActualApiResponseMetadata? InspectReturnStatementSyntax(
            in ApiControllerSymbolCache symbolCache,
            SemanticModel semanticModel,
            ReturnStatementSyntax returnStatementSyntax,
            CancellationToken cancellationToken)
        {
            var returnExpression = returnStatementSyntax.Expression;
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
                var defaultStatusCode = GetDefaultStatusCode(defaultStatusCodeAttribute);
                if (defaultStatusCode == null)
                {
                    // Unable to read the status code even though the attribute exists.
                    return null;
                }

                return new ActualApiResponseMetadata(returnStatementSyntax, defaultStatusCode.Value);
            }

            if (!symbolCache.IActionResult.IsAssignableFrom(statementReturnType))
            {
                // Return expression does not have a DefaultStatusCodeAttribute and it is not
                // an instance of IActionResult. Must be returning the "model".
                return new ActualApiResponseMetadata(returnStatementSyntax);
            }

            int statusCode;
            switch (returnExpression)
            {
                case InvocationExpressionSyntax invocation:
                    // Covers the 'return StatusCode(200)' case.
                    if (TryGetParameterStatusCode(symbolCache, semanticModel, invocation.Expression, invocation.ArgumentList, cancellationToken, out statusCode))
                    {
                        return new ActualApiResponseMetadata(returnStatementSyntax, statusCode);
                    }
                    break;

                case ObjectCreationExpressionSyntax creation:
                    // Covers the 'return new ObjectResult(...) { StatusCode = 200 }' case.
                    if (TryGetInitializerStatusCode(symbolCache, semanticModel, creation.Initializer, cancellationToken, out statusCode))
                    {
                        return new ActualApiResponseMetadata(returnStatementSyntax, statusCode);
                    }

                    // Covers the 'return new StatusCodeResult(200) case.
                    if (TryGetParameterStatusCode(symbolCache, semanticModel, creation, creation.ArgumentList, cancellationToken, out statusCode))
                    {
                        return new ActualApiResponseMetadata(returnStatementSyntax, statusCode);
                    }
                    break;
            }

            return null;
        }

        private static bool TryGetInitializerStatusCode(
            in ApiControllerSymbolCache symbolCache,
            SemanticModel semanticModel,
            InitializerExpressionSyntax initializer,
            CancellationToken cancellationToken,
            out int statusCode)
        {
            if (initializer == null)
            {
                statusCode = default;
                return false;
            }

            for (var i = 0; i < initializer.Expressions.Count; i++)
            {
                if (!(initializer.Expressions[i] is AssignmentExpressionSyntax assignment))
                {
                    continue;
                }

                if (assignment.Left is IdentifierNameSyntax identifier)
                {
                    var symbolInfo = semanticModel.GetSymbolInfo(identifier, cancellationToken);

                    if (symbolInfo.Symbol is IPropertySymbol property && IsInterfaceImplementation(property, symbolCache.StatusCodeActionResultStatusProperty))
                    {
                        return TryGetExpressionStatusCode(semanticModel, assignment.Right, cancellationToken, out statusCode);
                    }
                }
            }

            statusCode = default;
            return false;
        }

        private static bool IsInterfaceImplementation(IPropertySymbol property, IPropertySymbol statusCodeActionResultStatusProperty)
        {
            if (property.Name != statusCodeActionResultStatusProperty.Name)
            {
                return false;
            }

            for (var i = 0; i < property.ExplicitInterfaceImplementations.Length; i++)
            {
                if (property.ExplicitInterfaceImplementations[i] == statusCodeActionResultStatusProperty)
                {
                    return true;
                }
            }

            var implementedProperty = property.ContainingType.FindImplementationForInterfaceMember(statusCodeActionResultStatusProperty);
            return implementedProperty == property;
        }

        private static bool TryGetParameterStatusCode(
            in ApiControllerSymbolCache symbolCache,
            SemanticModel semanticModel,
            ExpressionSyntax expression,
            BaseArgumentListSyntax argumentList,
            CancellationToken cancellationToken,
            out int statusCode)
        {
            var symbolInfo = semanticModel.GetSymbolInfo(expression, cancellationToken);

            if (!(symbolInfo.Symbol is IMethodSymbol method))
            {
                statusCode = default;
                return false;
            }

            for (var i = 0; i < method.Parameters.Length; i++)
            {
                var parameter = method.Parameters[i];
                if (!parameter.HasAttribute(symbolCache.StatusCodeValueAttribute))
                {
                    continue;
                }


                var argument = argumentList.Arguments[parameter.Ordinal];
                return TryGetExpressionStatusCode(semanticModel, argument.Expression, cancellationToken, out statusCode);
            }

            statusCode = default;
            return false;
        }

        private static bool TryGetExpressionStatusCode(
            SemanticModel semanticModel,
            ExpressionSyntax expression,
            CancellationToken cancellationToken,
            out int statusCode)
        {
            if (expression is LiteralExpressionSyntax literal && literal.Token.Value is int literalStatusCode)
            {
                // Covers the 'return StatusCode(200)' case.
                statusCode = literalStatusCode;
                return true;
            }

            if (expression is IdentifierNameSyntax || expression is MemberAccessExpressionSyntax)
            {
                var symbolInfo = semanticModel.GetSymbolInfo(expression, cancellationToken);

                if (symbolInfo.Symbol is IFieldSymbol field && field.HasConstantValue && field.ConstantValue is int constantStatusCode)
                {
                    // Covers the 'return StatusCode(StatusCodes.Status200OK)' case.
                    // It also covers the 'return StatusCode(StatusCode)' case, where 'StatusCode' is a constant field.
                    statusCode = constantStatusCode;
                    return true;
                }

                if (symbolInfo.Symbol is ILocalSymbol local && local.HasConstantValue && local.ConstantValue is int localStatusCode)
                {
                    // Covers the 'return StatusCode(statusCode)' case, where 'statusCode' is a local constant.
                    statusCode = localStatusCode;
                    return true;
                }
            }

            statusCode = default;
            return false;
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