// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.AspNetCore.Analyzers.Infrastructure;
using Microsoft.AspNetCore.Http.RequestDelegateGenerator.StaticRouteHandlerModel;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.AspNetCore.Http.ValidationsGenerator;

public sealed partial class ValidationsGenerator : IIncrementalGenerator
{
    internal bool FindWithValidation(SyntaxNode syntaxNode, CancellationToken cancellationToken)
    {
        if (syntaxNode is InvocationExpressionSyntax
            && syntaxNode.TryGetMapMethodName(out var method)
            && method == "WithValidation")
        {
            return true;
        }
        return false;
    }

#pragma warning disable RSEXPERIMENTAL002 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    internal InterceptableLocation? TransformWithValidation(GeneratorSyntaxContext context, CancellationToken cancellationToken)
    {
        var node = (InvocationExpressionSyntax)context.Node;
        var semanticModel = context.SemanticModel;
        return semanticModel.GetInterceptableLocation(node, cancellationToken);
    }
#pragma warning restore RSEXPERIMENTAL002 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

    internal bool FindEndpoints(SyntaxNode syntaxNode, CancellationToken cancellationToken)
    {
        if (syntaxNode is InvocationExpressionSyntax
            && syntaxNode.TryGetMapMethodName(out var method))
        {
            return method == "MapMethods" || InvocationOperationExtensions.KnownMethods.Contains(method);
        }
        return false;
    }

    internal IInvocationOperation TransformEndpoints(GeneratorSyntaxContext context, CancellationToken cancellationToken)
    {
        var node = (InvocationExpressionSyntax)context.Node;
        var operation = context.SemanticModel.GetOperation(node, cancellationToken);
        AnalyzerDebug.Assert(operation != null, "Operation should not be null.");
        return (IInvocationOperation)operation;
    }

    internal ValidatableEndpoint ExtractValidatableEndpoint((IInvocationOperation Operation, RequiredSymbols RequiredSymbols) input, CancellationToken cancellationToken)
    {
        var endpointKey = input.Operation.GetEndpointKey();
        HashSet<ValidatableType> validatableTypes = new HashSet<ValidatableType>(ValidatableTypeComparer.Instance);
        var parameters = ExtractParameters(input.Operation, input.RequiredSymbols, ref validatableTypes);
        return new ValidatableEndpoint(endpointKey, parameters, [.. validatableTypes]);
    }
}
