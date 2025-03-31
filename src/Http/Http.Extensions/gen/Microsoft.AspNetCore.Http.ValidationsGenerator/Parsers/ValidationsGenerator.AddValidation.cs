// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using Microsoft.AspNetCore.Http.RequestDelegateGenerator.StaticRouteHandlerModel;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.AspNetCore.Http.ValidationsGenerator;

public sealed partial class ValidationsGenerator : IIncrementalGenerator
{
    internal bool FindAddValidation(SyntaxNode syntaxNode, CancellationToken cancellationToken)
    {
        if (syntaxNode is InvocationExpressionSyntax
            && syntaxNode.TryGetMapMethodName(out var method)
            && method == "AddValidation")
        {
            return true;
        }
        return false;
    }

    internal InterceptableLocation? TransformAddValidation(GeneratorSyntaxContext context, CancellationToken cancellationToken)
    {
        var node = (InvocationExpressionSyntax)context.Node;
        var semanticModel = context.SemanticModel;
        var symbol = semanticModel.GetSymbolInfo(node, cancellationToken).Symbol;
        if (symbol is not IMethodSymbol methodSymbol
            || methodSymbol.ContainingType.Name != "ValidationServiceCollectionExtensions"
            || methodSymbol.ContainingAssembly.Name != "Microsoft.AspNetCore.Http.Abstractions")
        {
            return null;
        }
        return semanticModel.GetInterceptableLocation(node, cancellationToken);
    }
}
