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
using Microsoft.CodeAnalysis.Formatting;

namespace Microsoft.AspNetCore.Analyzers.WebApplicationBuilder.Fixers;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class RerouteAuthorizationFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds { get; } =
        ImmutableArray.Create(
            DiagnosticDescriptors.ExplicitRoutingRequiredForAuthorization.Id,
            DiagnosticDescriptors.ReroutingMiddlewareMustPrecedeRouting.Id);

    public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null)
        {
            return;
        }

        foreach (var diagnostic in context.Diagnostics)
        {
            if (!diagnostic.Properties.TryGetValue(RerouteAuthorizationAnalyzer.FixKindKey, out var fixKind))
            {
                continue;
            }

            var title = fixKind == RerouteAuthorizationAnalyzer.InsertRoutingAfter
                ? "Add explicit routing after rerouting middleware"
                : "Move rerouting middleware before routing";

            context.RegisterCodeFix(
                CodeAction.Create(
                    title,
                    cancellationToken => ApplyFixAsync(context.Document, diagnostic, fixKind!, cancellationToken),
                    equivalenceKey: $"{diagnostic.Id}:{fixKind}"),
                diagnostic);
        }
    }

    private static async Task<Document> ApplyFixAsync(
        Document document,
        Diagnostic diagnostic,
        string fixKind,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null)
        {
            return document;
        }

        var invocation = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true)
            .FirstAncestorOrSelf<InvocationExpressionSyntax>();
        var expressionStatement = invocation?.FirstAncestorOrSelf<ExpressionStatementSyntax>();
        if (invocation?.Expression is not MemberAccessExpressionSyntax memberAccess || expressionStatement is null)
        {
            return document;
        }

        if (fixKind == RerouteAuthorizationAnalyzer.InsertRoutingAfter)
        {
            var useRoutingStatement = SyntaxFactory.ExpressionStatement(
                    SyntaxFactory.InvocationExpression(
                        SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            memberAccess.Expression.WithoutTrivia(),
                            SyntaxFactory.IdentifierName("UseRouting"))))
                .WithAdditionalAnnotations(Formatter.Annotation);

            return document.WithSyntaxRoot(InsertAfter(root, expressionStatement, useRoutingStatement));
        }

        return document.WithSyntaxRoot(MoveBeforePreviousStatement(root, expressionStatement));
    }

    private static SyntaxNode InsertAfter(
        SyntaxNode root,
        ExpressionStatementSyntax anchor,
        ExpressionStatementSyntax statement)
    {
        if (anchor.Parent is BlockSyntax block)
        {
            var index = block.Statements.IndexOf(anchor);
            return root.ReplaceNode(block, block.WithStatements(block.Statements.Insert(index + 1, statement)));
        }

        if (anchor.Parent is GlobalStatementSyntax globalStatement &&
            globalStatement.Parent is CompilationUnitSyntax compilationUnit)
        {
            var index = compilationUnit.Members.IndexOf(globalStatement);
            var newGlobalStatement = SyntaxFactory.GlobalStatement(statement);
            return root.ReplaceNode(
                compilationUnit,
                compilationUnit.WithMembers(compilationUnit.Members.Insert(index + 1, newGlobalStatement)));
        }

        return root;
    }

    private static SyntaxNode MoveBeforePreviousStatement(SyntaxNode root, ExpressionStatementSyntax statement)
    {
        if (statement.Parent is BlockSyntax block)
        {
            var index = block.Statements.IndexOf(statement);
            if (index <= 0)
            {
                return root;
            }

            var previous = block.Statements[index - 1];
            var reordered = block.Statements
                .Replace(previous, previous.WithLeadingTrivia(statement.GetLeadingTrivia()))
                .RemoveAt(index)
                .Insert(index - 1, statement.WithLeadingTrivia(previous.GetLeadingTrivia()));
            return root.ReplaceNode(block, block.WithStatements(reordered));
        }

        if (statement.Parent is GlobalStatementSyntax globalStatement &&
            globalStatement.Parent is CompilationUnitSyntax compilationUnit)
        {
            var index = compilationUnit.Members.IndexOf(globalStatement);
            if (index <= 0)
            {
                return root;
            }

            var previous = compilationUnit.Members[index - 1];
            var reordered = compilationUnit.Members
                .Replace(previous, previous.WithLeadingTrivia(globalStatement.GetLeadingTrivia()))
                .RemoveAt(index)
                .Insert(index - 1, globalStatement.WithLeadingTrivia(previous.GetLeadingTrivia()));
            return root.ReplaceNode(compilationUnit, compilationUnit.WithMembers(reordered));
        }

        return root;
    }
}
