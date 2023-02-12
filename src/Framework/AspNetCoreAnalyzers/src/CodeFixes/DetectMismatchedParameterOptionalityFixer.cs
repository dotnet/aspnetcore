// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.AspNetCore.Analyzers.RouteHandlers.Fixers;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public class DetectMismatchedParameterOptionalityFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(DiagnosticDescriptors.DetectMismatchedParameterOptionality.Id);

    public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        foreach (var diagnostic in context.Diagnostics)
        {
            context.RegisterCodeFix(
                CodeAction.Create("Fix mismatched route parameter and argument optionality",
                    cancellationToken => FixMismatchedParameterOptionalityAsync(diagnostic, context.Document, cancellationToken),
                    equivalenceKey: DiagnosticDescriptors.DetectMismatchedParameterOptionality.Id),
                diagnostic);
        }

        return Task.CompletedTask;
    }

    private static async Task<Document> FixMismatchedParameterOptionalityAsync(Diagnostic diagnostic, Document document, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null)
        {
            return document;
        }

        var param = root.FindNode(diagnostic.Location.SourceSpan);
        if (param is ParameterSyntax { Type: { } parameterType } parameterSyntax)
        {
            var newParam = parameterSyntax.WithType(SyntaxFactory.NullableType(parameterType));
            return document.WithSyntaxRoot(root.ReplaceNode(parameterSyntax, newParam));
        }

        return document;
    }
}
