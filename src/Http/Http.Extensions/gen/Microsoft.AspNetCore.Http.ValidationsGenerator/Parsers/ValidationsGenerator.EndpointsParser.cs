// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.AspNetCore.Analyzers.Infrastructure;
using Microsoft.AspNetCore.Http.RequestDelegateGenerator.StaticRouteHandlerModel;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.AspNetCore.Http.ValidationsGenerator;

public sealed partial class ValidationsGenerator : IIncrementalGenerator
{
    internal bool FindEndpoints(SyntaxNode syntaxNode, CancellationToken cancellationToken)
    {
        if (syntaxNode is InvocationExpressionSyntax
            && syntaxNode.TryGetMapMethodName(out var method))
        {
            return method == "MapMethods" || InvocationOperationExtensions.KnownMethods.Contains(method);
        }
        return false;
    }

    internal IInvocationOperation? TransformEndpoints(GeneratorSyntaxContext context, CancellationToken cancellationToken)
    {
        if (context.Node is not InvocationExpressionSyntax node)
        {
            return null;
        }
        var operation = context.SemanticModel.GetOperation(node, cancellationToken);
        AnalyzerDebug.Assert(operation != null, "Operation should not be null.");
        return operation is IInvocationOperation invocationOperation
            ? invocationOperation
            : null;
    }

    internal ImmutableArray<ValidatableType> ExtractValidatableEndpoint((IInvocationOperation? Operation, RequiredSymbols RequiredSymbols) input, CancellationToken cancellationToken)
    {
        AnalyzerDebug.Assert(input.Operation != null, "Operation should not be null.");
        var validatableTypes = ExtractValidatableTypes(input.Operation, input.RequiredSymbols);
        return validatableTypes;
    }
}
