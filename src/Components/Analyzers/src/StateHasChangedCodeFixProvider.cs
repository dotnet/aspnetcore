// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Composition;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.AspNetCore.Components.Analyzers;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(StateHasChangedCodeFixProvider)), Shared]
public class StateHasChangedCodeFixProvider : CodeFixProvider
{
    private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.UnnecessaryStateHasChangedCall_FixTitle), Resources.ResourceManager, typeof(Resources));

    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(DiagnosticDescriptors.UnnecessaryStateHasChangedCall.Id);

    public sealed override FixAllProvider GetFixAllProvider()
    {
        return WellKnownFixAllProviders.BatchFixer;
    }

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null)
        {
            return;
        }

        var diagnostic = context.Diagnostics.First();
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        var invocation = root.FindToken(diagnosticSpan.Start)
            .Parent?
            .AncestorsAndSelf()
            .OfType<InvocationExpressionSyntax>()
            .FirstOrDefault();

        if (invocation is null)
        {
            return;
        }

        var title = Title.ToString(CultureInfo.CurrentCulture);
        context.RegisterCodeFix(
            CodeAction.Create(
                title: title,
                createChangedDocument: cancellationToken => RemoveStateHasChangedCallAsync(context.Document, root, invocation, cancellationToken),
                equivalenceKey: title),
            diagnostic);
    }

    private static Task<Document> RemoveStateHasChangedCallAsync(Document document, SyntaxNode root, InvocationExpressionSyntax invocation, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        SyntaxNode nodeToRemove;
        if (invocation.Parent is ExpressionStatementSyntax expressionStatement)
        {
            nodeToRemove = expressionStatement;
        }
        else if (invocation.Parent is ArrowExpressionClauseSyntax { Parent: MethodDeclarationSyntax containingMethod })
        {
            nodeToRemove = containingMethod;
        }
        else
        {
            nodeToRemove = invocation;
        }

        var newRoot = root.RemoveNode(nodeToRemove, SyntaxRemoveOptions.KeepExteriorTrivia);
        return Task.FromResult(newRoot is null ? document : document.WithSyntaxRoot(newRoot));
    }
}
