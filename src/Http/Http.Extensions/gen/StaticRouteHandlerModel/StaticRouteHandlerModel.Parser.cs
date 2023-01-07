// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.AspNetCore.Http.SourceGeneration.StaticRouteHandlerModel;

internal static class StaticRouteHandlerModelParser
{
    private const int _routePatternArgumentOrdinal = 1;
    private const int _routeHandlerArgumentOrdinal = 2;

    public static EndpointRoute GetEndpointRouteFromArgument(SyntaxToken routePattern)
    {
        return new EndpointRoute
        {
            RoutePattern = routePattern.ValueText
        };
    }

    public static EndpointResponse GetEndpointResponseFromMethod(IMethodSymbol method)
    {
        return new EndpointResponse
        {
            ContentType = "plain/text",
            ResponseType = method.ReturnType.ToString(),
        };
    }

    public static Endpoint GetEndpointFromOperation(IInvocationOperation operation)
    {
        if (!TryGetRouteHandlerPattern(operation, out var routeToken))
        {
            return null;
        }
        if (!TryGetRouteHandlerMethod(operation, out var method))
        {
            return null;
        }
        var filePath = operation.Syntax.SyntaxTree.FilePath;
        var span = operation.Syntax.SyntaxTree.GetLineSpan(operation.Syntax.Span);

        var invocationExpression = (InvocationExpressionSyntax)operation.Syntax;
        var httpMethod = ((IdentifierNameSyntax)((MemberAccessExpressionSyntax)invocationExpression.Expression).Name).Identifier.ValueText;

        return new Endpoint
        {
            Route = GetEndpointRouteFromArgument(routeToken),
            Response = GetEndpointResponseFromMethod(method),
            Location = (filePath, span.EndLinePosition.Line + 1),
            HttpMethod = httpMethod,
        };
    }

    private static bool TryGetRouteHandlerPattern(IInvocationOperation invocation, out SyntaxToken token)
    {
        IArgumentOperation? argumentOperation = null;
        foreach (var argument in invocation.Arguments)
        {
            if (argument.Parameter?.Ordinal == _routePatternArgumentOrdinal)
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

    private static bool TryGetRouteHandlerMethod(IInvocationOperation invocation, out IMethodSymbol method)
    {
        IArgumentOperation? argumentOperation = null;
        method = null;
        foreach (var argument in invocation.Arguments)
        {
            if (argument.Parameter?.Ordinal == _routeHandlerArgumentOrdinal)
            {
                argumentOperation = argument;
            }
        }

        if (argumentOperation is not null)
        {
            method = ResolveMethodFromOperation(argumentOperation);
            return true;
        }

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
