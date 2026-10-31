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

        var diagnostic = context.Diagnostics[0];
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

        // Derive indentation and line ending from the original statement so existing trivia is reused
        var leadingTrivia = expressionStatement.GetLeadingTrivia();
        var trailingTrivia = expressionStatement.GetTrailingTrivia();

        // Prefer the statement's own line ending, then any in the document, then CRLF.
        var eol = leadingTrivia.FirstOrDefault(t => t.IsKind(SyntaxKind.EndOfLineTrivia));
        if (eol == default)
        {
            eol = root.DescendantTrivia().FirstOrDefault(t => t.IsKind(SyntaxKind.EndOfLineTrivia));
            if (eol == default)
            {
                eol = SyntaxFactory.CarriageReturnLineFeed;
            }
        }

        var indentTrivia = leadingTrivia.Where(t => t.IsKind(SyntaxKind.WhitespaceTrivia)).LastOrDefault();
        var indent = indentTrivia == default ? "" : indentTrivia.ToFullString();

        // Use the document's indentation settings for the added indent level
        var options = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
        var useTabs = options.GetOption(FormattingOptions.UseTabs, LanguageNames.CSharp);
        var indentSize = options.GetOption(FormattingOptions.IndentationSize, LanguageNames.CSharp);
        var singleIndent = useTabs ? "\t" : new string(' ', indentSize);

        var indentList = SyntaxFactory.TriviaList(SyntaxFactory.Whitespace(indent));
        var innerIndent = SyntaxFactory.Whitespace(indent + singleIndent);
        var eolList = SyntaxFactory.TriviaList(eol);

        var tryStatement = SyntaxFactory.TryStatement(
            SyntaxFactory.Block(
                SyntaxFactory.SingletonList<StatementSyntax>(
                    expressionStatement
                        .WithLeadingTrivia(innerIndent)
                        .WithTrailingTrivia(eol)))
            .WithOpenBraceToken(SyntaxFactory.Token(indentList, SyntaxKind.OpenBraceToken, eolList))
            .WithCloseBraceToken(SyntaxFactory.Token(indentList, SyntaxKind.CloseBraceToken, eolList)),
            SyntaxFactory.SingletonList(
                SyntaxFactory.CatchClause()
                    .WithCatchKeyword(SyntaxFactory.Token(indentList, SyntaxKind.CatchKeyword, SyntaxFactory.TriviaList(SyntaxFactory.Space)))
                    .WithDeclaration(
                        SyntaxFactory.CatchDeclaration(
                            SyntaxFactory.ParseTypeName("Exception"))
                            .WithCloseParenToken(SyntaxFactory.Token(SyntaxTriviaList.Empty, SyntaxKind.CloseParenToken, eolList)))
                    .WithBlock(
                        SyntaxFactory.Block()
                            .WithOpenBraceToken(SyntaxFactory.Token(indentList, SyntaxKind.OpenBraceToken, eolList))
                            .WithCloseBraceToken(SyntaxFactory.Token(indentList, SyntaxKind.CloseBraceToken, SyntaxTriviaList.Empty)))),
            null)
        .WithTryKeyword(SyntaxFactory.Token(leadingTrivia, SyntaxKind.TryKeyword, eolList))
        .WithTrailingTrivia(trailingTrivia);

        var newRoot = root.ReplaceNode(expressionStatement, tryStatement);
        if (newRoot is null)
        {
            return document;
        }

        return document.WithSyntaxRoot(newRoot);
    }
}
