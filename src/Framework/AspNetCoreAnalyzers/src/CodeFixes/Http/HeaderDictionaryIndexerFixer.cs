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

namespace Microsoft.AspNetCore.Analyzers.Http.Fixers;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public class HeaderDictionaryIndexerFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(DiagnosticDescriptors.UseHeaderDictionaryPropertiesInsteadOfIndexer.Id);

    public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        foreach (var diagnostic in context.Diagnostics)
        {
            if (diagnostic.Properties.TryGetValue("HeaderName", out var headerName) &&
                diagnostic.Properties.TryGetValue("ResolvedPropertyName", out var resolvedPropertyName))
            {
                var title = $"Access header '{headerName}' with {resolvedPropertyName} property";
                context.RegisterCodeFix(
                    CodeAction.Create(title,
                        cancellationToken => FixHeaderDictionaryIndexer(diagnostic, context.Document, resolvedPropertyName!, cancellationToken),
                        equivalenceKey: title),
                    diagnostic);
            }
        }

        return Task.CompletedTask;
    }

    private static async Task<Document> FixHeaderDictionaryIndexer(Diagnostic diagnostic, Document document, string resolvedPropertyName, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        if (root == null)
        {
            return document;
        }

        var param = root.FindNode(diagnostic.Location.SourceSpan);
        if (param is ArgumentSyntax argumentSyntax)
        {
            param = argumentSyntax.Expression;
        }

        if (param is ElementAccessExpressionSyntax { Expression: { } expression } elementAccessExpressionSyntax)
        {
            var newExpression = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, expression, SyntaxFactory.IdentifierName(resolvedPropertyName));
            return document.WithSyntaxRoot(root.ReplaceNode(elementAccessExpressionSyntax, newExpression));
        }

        return document;
    }
}
