// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Threading;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Analyzers.DelegateEndpoints;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Editing;

namespace Microsoft.AspNetCore.Analyzers.DelegateEndpoints.Fixers;

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
                    cancellationToken => FixMismatchedParameterOptionality(context, cancellationToken),
                    equivalenceKey: DiagnosticDescriptors.DetectMismatchedParameterOptionality.Id),
                diagnostic);
        }

        return Task.CompletedTask;
    }

    private static async Task<Document> FixMismatchedParameterOptionality(CodeFixContext context, CancellationToken cancellationToken)
    {
        DocumentEditor editor = await DocumentEditor.CreateAsync(context.Document, cancellationToken);
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

        if (root == null)
        {
            return context.Document;
        }

        var diagnostic = context.Diagnostics.SingleOrDefault();

        if (diagnostic == null)
        {
            return context.Document;
        }

        var param = root.FindNode(diagnostic.Location.SourceSpan);
        if (param != null && param is ParameterSyntax parameterSyntax)
        {
            if (parameterSyntax.Type != null)
            {
                var newParam = parameterSyntax.WithType(SyntaxFactory.NullableType(parameterSyntax.Type));
                editor.ReplaceNode(parameterSyntax, newParam);
            }
        }

        return editor.GetChangedDocument();
    }
}
