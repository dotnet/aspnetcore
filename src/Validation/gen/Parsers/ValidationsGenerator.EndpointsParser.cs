// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.AspNetCore.Analyzers.Infrastructure;
using Microsoft.AspNetCore.Http.RequestDelegateGenerator.StaticRouteHandlerModel;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.Extensions.Validation;

public sealed partial class ValidationsGenerator : IIncrementalGenerator
{
    internal static bool FindEndpoints(SyntaxNode syntaxNode, CancellationToken cancellationToken)
    {
        if (syntaxNode is InvocationExpressionSyntax invocationExpressionSyntax
            && invocationExpressionSyntax.TryGetMapMethodName(out var method))
        {
            return method == "MapMethods" || InvocationOperationExtensions.KnownMethods.Contains(method);
        }
        return false;
    }

    internal static ImmutableArray<ValidatableType> TransformEndpoints(GeneratorSyntaxContext context, CancellationToken cancellationToken)
    {
        var operation = context.SemanticModel.GetOperation((InvocationExpressionSyntax)context.Node, cancellationToken);
        AnalyzerDebug.Assert(operation != null, "Operation should not be null.");
        return operation is IInvocationOperation invocationOperation
            ? ExtractValidatableTypes(invocationOperation)
            : default;
    }
}
