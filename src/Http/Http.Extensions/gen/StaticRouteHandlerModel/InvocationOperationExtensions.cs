// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.AspNetCore.Http.Generators.StaticRouteHandlerModel;

public static class InvocationOperationExtensions
{
    private const int RouteHandlerArgumentOrdinal = 2;

    public static bool TryGetRouteHandlerMethod(this IInvocationOperation invocation, out IMethodSymbol method)
    {
        foreach (var argument in invocation.Arguments)
        {
            if (argument.Parameter?.Ordinal == RouteHandlerArgumentOrdinal)
            {
                method = ResolveMethodFromOperation(argument);
                return true;
            }
        }
        method = null;
        return false;
    }

    private static IMethodSymbol ResolveMethodFromOperation(IOperation operation) => operation switch
    {
        IArgumentOperation argument => ResolveMethodFromOperation(argument.Value),
        IConversionOperation conv => ResolveMethodFromOperation(conv.Operand),
        IDelegateCreationOperation del => ResolveMethodFromOperation(del.Target),
        IFieldReferenceOperation { Field.IsReadOnly: true } f when ResolveDeclarationOperation(f.Field, operation.SemanticModel) is IOperation op =>
            ResolveMethodFromOperation(op),
        IAnonymousFunctionOperation anon => anon.Symbol,
        ILocalFunctionOperation local => local.Symbol,
        IMethodReferenceOperation method => method.Method,
        IParenthesizedOperation parenthesized => ResolveMethodFromOperation(parenthesized.Operand),
        _ => null
    };

    private static IOperation ResolveDeclarationOperation(ISymbol symbol, SemanticModel semanticModel)
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
                var operation = semanticModel.GetOperation(expr);

                if (operation is not null)
                {
                    return operation;
                }
            }
        }

        return null;
    }
}
