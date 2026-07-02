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
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;

namespace Microsoft.AspNetCore.Components.Analyzers;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(JSInteropCodeFixProvider)), Shared]
public sealed class JSInteropCodeFixProvider : CodeFixProvider
{
    private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.UnguardedJSInteropCall_FixTitle), Resources.ResourceManager, typeof(Resources));

    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(DiagnosticDescriptors.UnguardedJSInteropCall.Id);

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

        if (invocation.Ancestors().OfType<AnonymousFunctionExpressionSyntax>().Any())
        {
            return;
        }

        var statement = invocation.FirstAncestorOrSelf<StatementSyntax>();
        if (statement is not ExpressionStatementSyntax expressionStatement)
        {
            return;
        }

        var title = Title.ToString(CultureInfo.CurrentCulture);
        context.RegisterCodeFix(
            CodeAction.Create(
                title: title,
                createChangedDocument: cancellationToken => TryCatchWrapJSInteropCallAsync(context.Document, root, expressionStatement, cancellationToken),
                equivalenceKey: title),
            diagnostic);
    }

    private static async Task<Document> TryCatchWrapJSInteropCallAsync(Document document, SyntaxNode root, ExpressionStatementSyntax expressionStatement, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var tryStatement =
            SyntaxFactory.TryStatement(
                SyntaxFactory.Block(expressionStatement),
                SyntaxFactory.SingletonList(
                    SyntaxFactory.CatchClause()
                        .WithDeclaration(
                            SyntaxFactory.CatchDeclaration(
                                SyntaxFactory.ParseTypeName("Exception")))
                        .WithBlock(SyntaxFactory.Block())), null)
            .WithAdditionalAnnotations(Formatter.Annotation);

        var newRoot = root.ReplaceNode(expressionStatement, tryStatement);
        if (newRoot is null)
        {
            return document;
        }

        var newDocument = document.WithSyntaxRoot(newRoot);
        var formattedRoot = await Formatter.FormatAsync(newDocument, Formatter.Annotation, cancellationToken: cancellationToken).ConfigureAwait(false);

        return formattedRoot;
    }
}
