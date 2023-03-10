// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.AspNetCore.Http.Generators.StaticRouteHandlerModel;

internal static class InvocationOperationExtensions
{
    private const int RoutePatternArgumentOrdinal = 1;
    private const int RouteHandlerArgumentOrdinal = 2;

    public static bool TryGetRouteHandlerMethod(this IInvocationOperation invocation, SemanticModel semanticModel, out IMethodSymbol? method)
    {
        foreach (var argument in invocation.Arguments)
        {
            if (argument.Parameter?.Ordinal == RouteHandlerArgumentOrdinal)
            {
                method = ResolveMethodFromOperation(argument, semanticModel);
                return true;
            }
        }
        method = null;
        return false;
    }

    public static bool TryGetRouteHandlerPattern(this IInvocationOperation invocation, out SyntaxToken token)
    {
        IArgumentOperation? argumentOperation = null;
        foreach (var argument in invocation.Arguments)
        {
            if (argument.Parameter?.Ordinal == RoutePatternArgumentOrdinal)
            {
                argumentOperation = argument;
            }
        }
        if (argumentOperation?.Syntax is not ArgumentSyntax routePatternArgumentSyntax ||
            routePatternArgumentSyntax.Expression is not LiteralExpressionSyntax routePatternArgumentLiteralSyntax)
        {
            token = default;
            return false;
        }
        token = routePatternArgumentLiteralSyntax.Token;
        return true;
    }

    private static IMethodSymbol? ResolveMethodFromOperation(IOperation operation, SemanticModel semanticModel) => operation switch
    {
        IArgumentOperation argument => ResolveMethodFromOperation(argument.Value, semanticModel),
        IConversionOperation conv => ResolveMethodFromOperation(conv.Operand, semanticModel),
        IDelegateCreationOperation del => ResolveMethodFromOperation(del.Target, semanticModel),
        IFieldReferenceOperation { Field.IsReadOnly: true } f when ResolveDeclarationOperation(f.Field, semanticModel) is IOperation op =>
            ResolveMethodFromOperation(op, semanticModel),
        IAnonymousFunctionOperation anon => anon.Symbol,
        ILocalFunctionOperation local => local.Symbol,
        IMethodReferenceOperation method => method.Method,
        IParenthesizedOperation parenthesized => ResolveMethodFromOperation(parenthesized.Operand, semanticModel),
        _ => null
    };

    private static IOperation? ResolveDeclarationOperation(ISymbol symbol, SemanticModel? semanticModel)
    {
        foreach (var syntaxReference in symbol.DeclaringSyntaxReferences)
        {
            var syn = syntaxReference.GetSyntax();

            if (syn is VariableDeclaratorSyntax
                {
                    Initializer:
                    {
                        Value: var expr
                    }
                })
            {
                // Use the correct semantic model based on the syntax tree
                var targetSemanticModel = semanticModel.Compilation.GetSemanticModel(expr.SyntaxTree);
                var operation = targetSemanticModel?.GetOperation(expr);

                if (operation is not null)
                {
                    return operation;
                }
            }
        }

        return null;
    }
}
