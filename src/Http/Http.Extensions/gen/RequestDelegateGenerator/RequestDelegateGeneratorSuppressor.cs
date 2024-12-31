// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.AspNetCore.App.Analyzers.Infrastructure;
using Microsoft.AspNetCore.Http.RequestDelegateGenerator.StaticRouteHandlerModel;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

/*
 * This class contains the logic for suppressing diagnostics that are
 * emitted by the linker analyzers when encountering the framework-provided
 * `Map` invocations. Pending the completion of https://github.com/dotnet/roslyn/issues/68669,
 * this workaround is necessary to apply these suppressions for `Map` invocations that the RDG
 * is able to generate code at compile time for that the analyzer is not able to resolve.
 */

namespace Microsoft.AspNetCore.Http.RequestDelegateGenerator;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class RequestDelegateGeneratorSuppressor : DiagnosticSuppressor
{
    private static readonly SuppressionDescriptor SuppressRUCDiagnostic = new(
        id: "RDGS001",
        suppressedDiagnosticId: "IL2026",
        justification: "The target method has been intercepted by a statically generated variant.");

    private static readonly SuppressionDescriptor SuppressRDCDiagnostic = new(
        id: "RDGS002",
        suppressedDiagnosticId: "IL3050",
        justification: "The target method has been intercepted by a statically generated variant.");

    public override void ReportSuppressions(SuppressionAnalysisContext context)
    {
        foreach (var diagnostic in context.ReportedDiagnostics)
        {
            if (diagnostic.Id != SuppressRDCDiagnostic.SuppressedDiagnosticId && diagnostic.Id != SuppressRUCDiagnostic.SuppressedDiagnosticId)
            {
                continue;
            }

            var location = diagnostic.AdditionalLocations.Count > 0
                ? diagnostic.AdditionalLocations[0]
                : diagnostic.Location;

            if (location.SourceTree is not { } sourceTree
                || sourceTree.GetRoot().FindNode(location.SourceSpan) is not InvocationExpressionSyntax node
                || !node.TryGetMapMethodName(out var method)
                || !InvocationOperationExtensions.KnownMethods.Contains(method))
            {
                continue;
            }

            var semanticModel = context.GetSemanticModel(sourceTree);
            var operation = semanticModel.GetOperation(node, context.CancellationToken);
            var wellKnownTypes = WellKnownTypes.GetOrCreate(semanticModel.Compilation);
            if (operation.IsValidOperation(wellKnownTypes, out var invocationOperation))
            {
                var endpoint = new Endpoint(invocationOperation, wellKnownTypes, semanticModel);
                if (endpoint.Diagnostics.Count == 0)
                {
                    var targetSuppression = diagnostic.Id == SuppressRUCDiagnostic.SuppressedDiagnosticId
                        ? SuppressRUCDiagnostic
                        : SuppressRDCDiagnostic;
                    context.ReportSuppression(Suppression.Create(targetSuppression, diagnostic));
                }
            }
        }
    }
    public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions => ImmutableArray.Create(SuppressRUCDiagnostic, SuppressRDCDiagnostic);
}
