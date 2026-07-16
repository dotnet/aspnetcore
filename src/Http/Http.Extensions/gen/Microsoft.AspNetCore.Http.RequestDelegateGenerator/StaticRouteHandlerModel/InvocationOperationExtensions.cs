// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.App.Analyzers.Infrastructure;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.AspNetCore.Http.RequestDelegateGenerator.StaticRouteHandlerModel;

internal static class InvocationOperationExtensions
{
    public static readonly string[] KnownMethods =
    {
        "MapGet",
        "MapPost",
        "MapPut",
        "MapDelete",
        "MapPatch",
        "Map",
        "MapMethods",
        "MapFallback"
    };

    public static bool IsValidOperation(this IOperation? operation, WellKnownTypes wellKnownTypes, [NotNullWhen(true)] out IInvocationOperation? invocationOperation)
    {
        invocationOperation = null;
        if (operation is IInvocationOperation targetOperation &&
            targetOperation.TargetMethod.ContainingNamespace is { Name: "Builder", ContainingNamespace: { Name: "AspNetCore", ContainingNamespace: { Name: "Microsoft", ContainingNamespace.IsGlobalNamespace: true } } } &&
            targetOperation.TargetMethod.ContainingAssembly.Name is "Microsoft.AspNetCore.Routing" &&
            targetOperation.TryGetRouteHandlerArgument(out var routeHandlerParameter) &&
            routeHandlerParameter is { Parameter.Type: {} delegateType } &&
            SymbolEqualityComparer.Default.Equals(delegateType, wellKnownTypes.Get(WellKnownTypeData.WellKnownType.System_Delegate)))
        {
            invocationOperation = targetOperation;
            return true;
        }
        return false;
    }

    public static bool TryGetRouteHandlerMethod(this IInvocationOperation invocation, SemanticModel semanticModel, bool mustPreserveParameterNames, [NotNullWhen(true)] out IMethodSymbol? method)
    {
        method = null;
        if (invocation.TryGetRouteHandlerArgument(out var argument))
        {
            method = ResolveMethodFromOperation(argument, semanticModel, mustPreserveParameterNames);
            return method is not null;
        }
        return false;
    }

    public static bool TryGetRouteHandlerArgument(this IInvocationOperation invocation, [NotNullWhen(true)] out IArgumentOperation? argumentOperation)
    {
        argumentOperation = null;
        // Route handler argument is assumed to be the last parameter provided to
        // the Map methods.
        var routeHandlerArgumentOrdinal = invocation.Arguments.Length - 1;
        foreach (var argument in invocation.Arguments)
        {
            if (argument.Parameter?.Ordinal == routeHandlerArgumentOrdinal)
            {
                argumentOperation = argument;
                return true;
            }
        }
        return false;
    }

    public static bool TryGetMapMethodName(this SyntaxNode node, out string? methodName)
    {
        methodName = default;
        return node is InvocationExpressionSyntax invocation && invocation.TryGetMapMethodName(out methodName);
    }

    public static bool TryGetMapMethodName(this InvocationExpressionSyntax node, out string? methodName)
    {
        methodName = default;
        // Given an invocation like app.MapGet, app.Map, app.MapFallback, etc. get
        // the value of the Map method being access on the the WebApplication `app`.
        if (node.Expression is MemberAccessExpressionSyntax { Name.Identifier.ValueText: var method })
        {
            methodName = method;
            return true;
        }

        return false;
    }

    private static IMethodSymbol? ResolveMethodFromOperation(IOperation operation, SemanticModel semanticModel, bool mustPreserveParameterNames) => operation switch
    {
        IArgumentOperation argument => ResolveMethodFromOperation(argument.Value, semanticModel, mustPreserveParameterNames),
        IConversionOperation conv => ResolveMethodFromOperation(conv.Operand, semanticModel, mustPreserveParameterNames),
        IDelegateCreationOperation del => ResolveMethodFromOperation(del.Target, semanticModel, mustPreserveParameterNames),
        IFieldReferenceOperation { Field.IsReadOnly: true } f when ResolveDeclarationOperation(f.Field, semanticModel) is IOperation op =>
            ResolveMethodFromOperation(op, semanticModel, mustPreserveParameterNames),
        IAnonymousFunctionOperation anon => anon.Symbol,
        ILocalFunctionOperation local => local.Symbol,
        IMethodReferenceOperation method => method.Method,
        IParenthesizedOperation parenthesized => ResolveMethodFromOperation(parenthesized.Operand, semanticModel, mustPreserveParameterNames),
        _ => ResolveMethodFromType(operation.Type, mustPreserveParameterNames),
    };

    private static IMethodSymbol? ResolveMethodFromType(ITypeSymbol? type, bool mustPreserveParameterNames)
    {
        if (type is not INamedTypeSymbol { DelegateInvokeMethod: { } delegateInvokeMethod })
        {
            return null;
        }

        // If we have the following code:
        // static Func<string, string> GetHandler() => id => $"Hello, {id}!";
        // app.MapGet("/hello/{id}", GetHandler());
        // Then getting the DelegateInvokeMethod from GetHandler isn't going to preserve the "id" parameter name.
        // We mostly just have the Func<string, string> **type** which doesn't carry that name.
        // If the caller cares about getting the correct parameter name of the method (e.g, RDG), we return null in this case.
        // If the caller doesn't care, and cares only about getting the parameter type (e.g, validation), we return the delegate invoke method.
        // However, if we have just a Func<string> (delegate that returns a string but doesn't accept any parameters),
        // then we can return the invoke method both in RDG and validations.
        if (!mustPreserveParameterNames || delegateInvokeMethod.Parameters.Length == 0)
        {
            return delegateInvokeMethod;
        }

        return null;
    }

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
                var targetSemanticModel = semanticModel?.Compilation.GetSemanticModel(expr.SyntaxTree);
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
