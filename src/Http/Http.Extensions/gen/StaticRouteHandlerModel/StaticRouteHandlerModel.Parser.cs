// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.AspNetCore.Http.SourceGeneration.StaticRouteHandlerModel;

internal static class StaticRouteHandlerModelParser
{
    public static EndpointRoute GetEndpointRouteFromArgument(IArgumentOperation argumentOperation)
    {
        var syntax = argumentOperation.Syntax as ArgumentSyntax;
        var expression = syntax.Expression as LiteralExpressionSyntax;
        return new EndpointRoute
        {
            RoutePattern = expression.Token.ValueText
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
        var routePatternArgument = operation.Arguments[1];
        var method = ResolveMethodFromOperation(operation.Arguments[2]);
        var filePath = operation.Syntax.SyntaxTree.FilePath;
        var span = operation.Syntax.SyntaxTree.GetLineSpan(operation.Syntax.Span);

        var invocationExpression = (InvocationExpressionSyntax)operation.Syntax;
        var httpMethod = ((IdentifierNameSyntax)((MemberAccessExpressionSyntax)invocationExpression.Expression).Name).Identifier.ValueText;

        return new Endpoint
        {
            Route = GetEndpointRouteFromArgument(routePatternArgument),
            Response = GetEndpointResponseFromMethod(method),
            Location = (filePath, span.EndLinePosition.Line + 1),
            HttpMethod = httpMethod,
        };
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
