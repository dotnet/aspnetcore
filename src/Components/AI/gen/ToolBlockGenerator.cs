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
        // Step 1: Filter for classes with [ToolBlock] attribute
        var candidates = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "Microsoft.AspNetCore.Components.AI.ToolBlockAttribute",
                predicate: static (node, _) => node is ClassDeclarationSyntax,
                transform: static (ctx, ct) => ToolBlockParser.Parse(ctx, ct))
            .Where(static c => c is not null)
            .Select(static (c, _) => c!);

        // Step 2: Emit handler source for each candidate
        context.RegisterSourceOutput(candidates, static (spc, candidate) =>
        {
            ToolBlockEmitter.EmitHandler(spc, candidate);
        });

        // Step 3: Collect all candidates and emit the aggregate registration
        var allCandidates = candidates.Collect();
        context.RegisterSourceOutput(allCandidates, static (spc, candidates) =>
        {
            ToolBlockEmitter.EmitRegistration(spc, candidates);
        });
    }
}
