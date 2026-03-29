// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.Extensions.Validation;

public sealed partial class ValidationsGenerator : IIncrementalGenerator
{
    internal bool FindAddValidationOptionsConfiguration(SyntaxNode syntaxNode, CancellationToken cancellationToken)
    {
        // Look for invocation expressions like: options.IncludeInternalTypes()
        // or lambda expressions containing such invocations
        return syntaxNode is InvocationExpressionSyntax or SimpleLambdaExpressionSyntax or ParenthesizedLambdaExpressionSyntax;
    }

    internal bool TransformIncludeInternalTypes(GeneratorSyntaxContext context, CancellationToken cancellationToken)
    {
        var node = context.Node;
        var semanticModel = context.SemanticModel;

        // Handle direct invocation: options.IncludeInternalTypes()
        if (node is InvocationExpressionSyntax invocation)
        {
            if (IsIncludeInternalTypesInvocation(invocation, semanticModel, cancellationToken))
            {
                return true;
            }
        }

        // Handle simple lambda: options => options.IncludeInternalTypes()
        if (node is SimpleLambdaExpressionSyntax simpleLambda)
        {
            return ContainsIncludeInternalTypesInvocation(simpleLambda, semanticModel, cancellationToken);
        }

        // Handle parenthesized lambda: (options) => options.IncludeInternalTypes()
        if (node is ParenthesizedLambdaExpressionSyntax parenthesizedLambda)
        {
            return ContainsIncludeInternalTypesInvocation(parenthesizedLambda, semanticModel, cancellationToken);
        }

        return false;
    }

    private static bool IsIncludeInternalTypesInvocation(
        InvocationExpressionSyntax invocation,
        SemanticModel semanticModel,
        CancellationToken cancellationToken)
    {
        // Check if this is an invocation of IncludeInternalTypes method
        if (invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
            memberAccess.Name.Identifier.Text == "IncludeInternalTypes")
        {
            // Verify it's the ValidationOptions.IncludeInternalTypes method
            var symbolInfo = semanticModel.GetSymbolInfo(memberAccess, cancellationToken);
            if (symbolInfo.Symbol is IMethodSymbol methodSymbol &&
                methodSymbol.Name == "IncludeInternalTypes" &&
                methodSymbol.ContainingType?.ToDisplayString() == "Microsoft.Extensions.Validation.ValidationOptions")
            {
                return true;
            }
        }

        return false;
    }

    private static bool ContainsIncludeInternalTypesInvocation(
        SimpleLambdaExpressionSyntax lambda,
        SemanticModel semanticModel,
        CancellationToken cancellationToken)
    {
        // Look for invocation expressions in the lambda body
        if (lambda.Body is InvocationExpressionSyntax invocation)
        {
            return IsIncludeInternalTypesInvocation(invocation, semanticModel, cancellationToken);
        }

        // Handle block lambdas: options => { options.IncludeInternalTypes(); return options; }
        if (lambda.Body is BlockSyntax block)
        {
            foreach (var statement in block.Statements)
            {
                if (statement is ExpressionStatementSyntax exprStatement &&
                    exprStatement.Expression is InvocationExpressionSyntax blockInvocation &&
                    IsIncludeInternalTypesInvocation(blockInvocation, semanticModel, cancellationToken))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool ContainsIncludeInternalTypesInvocation(
        ParenthesizedLambdaExpressionSyntax lambda,
        SemanticModel semanticModel,
        CancellationToken cancellationToken)
    {
        // Look for invocation expressions in the lambda body
        if (lambda.Body is InvocationExpressionSyntax invocation)
        {
            return IsIncludeInternalTypesInvocation(invocation, semanticModel, cancellationToken);
        }

        // Handle block lambdas: (options) => { options.IncludeInternalTypes(); return options; }
        if (lambda.Body is BlockSyntax block)
        {
            foreach (var statement in block.Statements)
            {
                if (statement is ExpressionStatementSyntax exprStatement &&
                    exprStatement.Expression is InvocationExpressionSyntax blockInvocation &&
                    IsIncludeInternalTypesInvocation(blockInvocation, semanticModel, cancellationToken))
                {
                    return true;
                }
            }
        }

        return false;
    }
}
