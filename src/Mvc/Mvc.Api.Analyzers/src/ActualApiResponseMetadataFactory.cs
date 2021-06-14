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
    public static class ActualApiResponseMetadataFactory
    {
        private static readonly Func<SyntaxNode, bool> _shouldDescendIntoChildren = ShouldDescendIntoChildren;

        /// <summary>
        /// This method looks at individual return statments and attempts to parse the status code and the return type.
        /// Given a <see cref="MethodDeclarationSyntax"/> for an action, this method inspects return statements in the body.
        /// If the returned type is not assignable from IActionResult, it assumes that an "object" value is being returned. e.g. return new Person();
        /// For return statements returning an action result, it attempts to infer the status code and return type. Helper methods in controller,
        /// values set in initializer and new-ing up an IActionResult instance are supported.
        /// </summary>
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
                if (returnStatementSyntax.IsMissing || returnStatementSyntax.Expression == null || returnStatementSyntax.Expression.IsMissing)
                {
                    // Ignore malformed return statements.
                    allReturnStatementsReadable = false;
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
            if (typeInfo.Type == null || typeInfo.Type.TypeKind == TypeKind.Error)
            {
                return null;
            }

            var statementReturnType = typeInfo.Type;

            if (!symbolCache.IActionResult.IsAssignableFrom(statementReturnType))
            {
                // Return expression is not an instance of IActionResult. Must be returning the "model".
                return new ActualApiResponseMetadata(returnStatementSyntax, statementReturnType);
            }

            var defaultStatusCodeAttribute = statementReturnType
                .GetAttributes(symbolCache.DefaultStatusCodeAttribute, inherit: true)
                .FirstOrDefault();

            var statusCode = GetDefaultStatusCode(defaultStatusCodeAttribute);
            ITypeSymbol? returnType = null;
            switch (returnExpression)
            {
                case InvocationExpressionSyntax invocation:
                    {
                        // Covers the 'return StatusCode(200)' case.
                        var result = InspectMethodArguments(semanticModel, invocation.Expression, invocation.ArgumentList, cancellationToken);
                        statusCode = result.statusCode ?? statusCode;
                        returnType = result.returnType;
                        break;
                    }

                case ObjectCreationExpressionSyntax creation:
                    {
                        // Read values from 'return new StatusCodeResult(200) case.
                        var result = InspectMethodArguments(semanticModel, creation, creation.ArgumentList, cancellationToken);
                        statusCode = result.statusCode ?? statusCode;
                        returnType = result.returnType;

                        // Read values from property assignments e.g. 'return new ObjectResult(...) { StatusCode = 200 }'.
                        // Property assignments override constructor assigned values and defaults.
                        result = InspectInitializers(symbolCache, semanticModel, creation.Initializer, cancellationToken);
                        statusCode = result.statusCode ?? statusCode;
                        returnType = result.returnType ?? returnType;
                        break;
                    }
            }

            if (statusCode == null)
            {
                return null;
            }

            return new ActualApiResponseMetadata(returnStatementSyntax, statusCode.Value, returnType);
        }

        private static (int? statusCode, ITypeSymbol? returnType) InspectInitializers(
            in ApiControllerSymbolCache symbolCache,
            SemanticModel semanticModel,
            InitializerExpressionSyntax? initializer,
            CancellationToken cancellationToken)
        {
            int? statusCode = null;
            ITypeSymbol? typeSymbol = null;

            for (var i = 0; initializer != null && i < initializer.Expressions.Count; i++)
            {
                var expression = initializer.Expressions[i];

                if (!(expression is AssignmentExpressionSyntax assignment) ||
                    !(assignment.Left is IdentifierNameSyntax identifier))
                {
                    continue;
                }

                var symbolInfo = semanticModel.GetSymbolInfo(identifier, cancellationToken);
                if (symbolInfo.Symbol is IPropertySymbol property)
                {
                    if (IsInterfaceImplementation(property, symbolCache.StatusCodeActionResultStatusProperty) &&
                        TryGetExpressionStatusCode(semanticModel, assignment.Right, cancellationToken, out var statusCodeValue))
                    {
                        // Look for assignments to IStatusCodeActionResult.StatusCode
                        statusCode = statusCodeValue;
                    }
                    else if (HasAttributeNamed(property, ApiSymbolNames.ActionResultObjectValueAttribute))
                    {
                        // Look for assignment to a property annotated with [ActionResultObjectValue]
                        typeSymbol = GetExpressionObjectType(semanticModel, assignment.Right, cancellationToken);
                    }
                }
            }

            return (statusCode, typeSymbol);
        }

        private static (int? statusCode, ITypeSymbol? returnType) InspectMethodArguments(
            SemanticModel semanticModel,
            ExpressionSyntax expression,
            BaseArgumentListSyntax argumentList,
            CancellationToken cancellationToken)
        {
            int? statusCode = null;
            ITypeSymbol? typeSymbol = null;

            var symbolInfo = semanticModel.GetSymbolInfo(expression, cancellationToken);

            if (symbolInfo.Symbol is IMethodSymbol method)
            {
                for (var i = 0; i < method.Parameters.Length; i++)
                {
                    var parameter = method.Parameters[i];
                    if (HasAttributeNamed(parameter, ApiSymbolNames.ActionResultStatusCodeAttribute))
                    {
                        var argument = argumentList.Arguments[parameter.Ordinal];
                        if (TryGetExpressionStatusCode(semanticModel, argument.Expression, cancellationToken, out var statusCodeValue))
                        {
                            statusCode = statusCodeValue;
                        }
                    }

                    if (HasAttributeNamed(parameter, ApiSymbolNames.ActionResultObjectValueAttribute))
                    {
                        var argument = argumentList.Arguments[parameter.Ordinal];
                        typeSymbol = GetExpressionObjectType(semanticModel, argument.Expression, cancellationToken);
                    }
                }
            }

            return (statusCode, typeSymbol);
        }

        private static ITypeSymbol? GetExpressionObjectType(SemanticModel semanticModel, ExpressionSyntax expression, CancellationToken cancellationToken)
        {
            var typeInfo = semanticModel.GetTypeInfo(expression, cancellationToken);

            return typeInfo.Type;
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

        private static bool IsInterfaceImplementation(IPropertySymbol property, IPropertySymbol statusCodeActionResultStatusProperty)
        {
            if (property.Name != statusCodeActionResultStatusProperty.Name)
            {
                return false;
            }

            for (var i = 0; i < property.ExplicitInterfaceImplementations.Length; i++)
            {
                if (SymbolEqualityComparer.Default.Equals(property.ExplicitInterfaceImplementations[i], statusCodeActionResultStatusProperty))
                {
                    return true;
                }
            }

            var implementedProperty = property.ContainingType.FindImplementationForInterfaceMember(statusCodeActionResultStatusProperty);
            return SymbolEqualityComparer.Default.Equals(implementedProperty, property);
        }

        private static bool HasAttributeNamed(ISymbol symbol, string attributeName)
        {
            var attributes = symbol.GetAttributes();
            var length = attributes.Length;
            for (var i = 0; i < length; i++)
            {
                if (attributes[i].AttributeClass.Name == attributeName)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
