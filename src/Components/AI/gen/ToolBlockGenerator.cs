// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.AspNetCore.Components.AI.SourceGenerators;

[Generator]
public class ToolBlockGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Step 1: Parse classes with [ToolBlock] into an equatable parse result
        // (candidate and/or diagnostics).
        var parseResults = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "Microsoft.AspNetCore.Components.AI.ToolBlockAttribute",
                predicate: static (node, _) => node is ClassDeclarationSyntax,
                transform: static (ctx, ct) => ToolBlockParser.Parse(ctx, ct));

        // Step 2: Report per-declaration diagnostics and emit a handler for each valid candidate.
        context.RegisterSourceOutput(parseResults, static (spc, result) =>
        {
            foreach (var info in result.Diagnostics)
            {
                spc.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.ById(info.DescriptorId),
                    info.Location?.ToLocation() ?? Location.None,
                    info.MessageArg));
            }

            if (result.Candidate is not null)
            {
                ToolBlockEmitter.EmitHandler(spc, result.Candidate);
            }
        });

        // Step 3: Collect all valid candidates and emit the aggregate registration.
        var candidates = parseResults
            .Where(static r => r.Candidate is not null)
            .Select(static (r, _) => r.Candidate!)
            .Collect();
        context.RegisterSourceOutput(candidates, static (spc, candidates) =>
        {
            ToolBlockEmitter.EmitRegistration(spc, candidates);
        });
    }
}
