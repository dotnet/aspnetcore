// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.AspNetCore.SignalR.Client.SourceGenerator;

[Generator]
internal sealed partial class HubServerProxyGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var methodDeclaration = context.SyntaxProvider
            .CreateSyntaxProvider(static (s, _) => Parser.IsSyntaxTargetForAttribute(s),
                static (ctx, _) => Parser.GetSemanticTargetForAttribute(ctx))
            .Where(static m => m is not null)
            .Collect();

        var memberAccessExpressions = context.SyntaxProvider
            .CreateSyntaxProvider(static (s, _) => Parser.IsSyntaxTargetForGeneration(s),
                static (ctx, _) => Parser.GetSemanticTargetForGeneration(ctx))
            .Where(static m => m is not null);

        var compilationAndMethodDeclaration = context.CompilationProvider.Combine(methodDeclaration);

        var payload = compilationAndMethodDeclaration
            .Combine(memberAccessExpressions.Collect());

        context.RegisterSourceOutput(payload, static (spc, source) =>
            Execute(source.Left.Left, source.Left.Right, source.Right, spc));
    }

    private static void Execute(Compilation compilation, ImmutableArray<MethodDeclarationSyntax> methodDeclarationSyntaxes, ImmutableArray<MemberAccessExpressionSyntax> memberAccessExpressionSyntaxes, SourceProductionContext context)
    {
        var parser = new Parser(context, compilation);
        var spec = parser.Parse(methodDeclarationSyntaxes, memberAccessExpressionSyntaxes);
        var emitter = new Emitter(context, spec);
        emitter.Emit();
    }
}
