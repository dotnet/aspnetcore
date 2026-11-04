// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Threading;
using Microsoft.AspNetCore.Analyzers.Infrastructure;
using Microsoft.AspNetCore.App.Analyzers.Infrastructure;
using Microsoft.AspNetCore.Http.RequestDelegateGenerator.StaticRouteHandlerModel;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Http.RequestDelegateGenerator;

public sealed partial class RequestDelegateGenerator : IIncrementalGenerator
{
    internal static bool IsEndpointInvocation(SyntaxNode node, CancellationToken cancellationToken)
        => node.TryGetMapMethodName(out var method) && InvocationOperationExtensions.KnownMethods.Contains(method);

    internal static Endpoint? TransformEndpoint(GeneratorSyntaxContext context, CancellationToken cancellationToken)
    {
        var operation = context.SemanticModel.GetOperation(context.Node, cancellationToken);
        var wellKnownTypes = WellKnownTypes.GetOrCreate(context.SemanticModel.Compilation);
        if (operation.IsValidOperation(wellKnownTypes, out var invocationOperation))
        {
            return new Endpoint(invocationOperation, wellKnownTypes, context.SemanticModel);
        }
        return null;
    }
}
