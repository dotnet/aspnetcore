// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;

namespace Microsoft.AspNetCore.Components.Analyzers;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ForLoopIteratorInClosureCodeFixProvider)), Shared]
public class ForLoopIteratorInClosureCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds
        => ImmutableArray.Create(DiagnosticDescriptors.ForLoopIteratorVariableUsedInClosure.Id);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        SyntaxNode root = await context.Document.GetSyntaxRootAsync(context.CancellationToken);
        Diagnostic diagnostic = context.Diagnostics.First();
        SyntaxNode node = root.FindNode(diagnostic.Location.SourceSpan);

        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Introduce a local copy of the loop variable",
                createChangedDocument: c => ApplyFixAsync(context.Document, node, c),
                equivalenceKey: "ForLoopClosureFix"),
            diagnostic);
    }

    private static async Task<Document> ApplyFixAsync(Document document, SyntaxNode node, CancellationToken c)
    {
        throw new NotImplementedException();
    }
}
