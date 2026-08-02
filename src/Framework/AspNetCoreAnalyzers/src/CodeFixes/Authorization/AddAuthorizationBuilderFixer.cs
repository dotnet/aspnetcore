// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.AspNetCore.Analyzers.Authorization.Fixers;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class AddAuthorizationBuilderFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(DiagnosticDescriptors.UseAddAuthorizationBuilder.Id);

    public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root == null)
        {
            return;
        }

        var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        if (semanticModel == null)
        {
            return;
        }

        foreach (var diagnostic in context.Diagnostics)
        {
            if (CanReplaceWithAddAuthorizationBuilder(diagnostic, root, out var invocation))
            {
                const string title = "Use 'AddAuthorizationBuilder'";
                context.RegisterCodeFix(
                    CodeAction.Create(title,
                        cancellationToken => ReplaceWithAddAuthorizationBuilder(diagnostic, root, context.Document, invocation),
                        equivalenceKey: DiagnosticDescriptors.UseAddAuthorizationBuilder.Id),
                    diagnostic);
            }
        }
    }

    private static bool CanReplaceWithAddAuthorizationBuilder(Diagnostic diagnostic, SyntaxNode root, [NotNullWhen(true)] out InvocationExpressionSyntax? invocation)
    {
        invocation = null;

        var diagnosticTarget = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);

        if (diagnosticTarget is InvocationExpressionSyntax { ArgumentList.Arguments: { Count: 1 } arguments, Expression: MemberAccessExpressionSyntax { Name.Identifier: { } identifierToken } memberAccessExpression }
            && arguments[0].Expression is SimpleLambdaExpressionSyntax lambda)
        {
            IEnumerable<SyntaxNode> nodes;

            if (lambda.Body is BlockSyntax lambdaBlockBody)
            {
                nodes = lambdaBlockBody.DescendantNodes();
            }
            else if (lambda.Body is InvocationExpressionSyntax lambdaExpressionBody)
            {
                nodes = new[] { lambdaExpressionBody };
            }
            else
            {
                Debug.Assert(false, "AddAuthorizationBuilderAnalyzer should not have emitted a diagnostic.");
                return false;
            }

            var addAuthorizationBuilderMethod = memberAccessExpression.ReplaceToken(identifierToken,
                SyntaxFactory.Identifier("AddAuthorizationBuilder"));

            invocation = SyntaxFactory.InvocationExpression(addAuthorizationBuilderMethod);

            foreach (var configureAction in nodes)
            {
                if (configureAction is InvocationExpressionSyntax { ArgumentList.Arguments: { Count: 2 } configureArguments, Expression: MemberAccessExpressionSyntax { Name.Identifier.Text: "AddPolicy" } })
                {
                    invocation = ChainInvocation(
                        invocation,
                        "AddPolicy",
                        SyntaxFactory.ArgumentList(
                            SyntaxFactory.SeparatedList(configureArguments)));
                }
                else if (configureAction is AssignmentExpressionSyntax { Left: MemberAccessExpressionSyntax { Name.Identifier.Text: { } assignmentTargetName }, Right: { } assignmentExpression }
                    && assignmentTargetName is "DefaultPolicy" or "FallbackPolicy" or "InvokeHandlersAfterFailure")
                {
                    invocation = ChainInvocation(
                        invocation,
                        $"Set{assignmentTargetName}",
                        SyntaxFactory.ArgumentList(
                            SyntaxFactory.SingletonSeparatedList(
                                SyntaxFactory.Argument(assignmentExpression))));
                }
            }

            return true;
        }

        Debug.Assert(false, "AddAuthorizationBuilderAnalyzer should not have emitted a diagnostic.");
        return false;
    }

    private static InvocationExpressionSyntax ChainInvocation(
        InvocationExpressionSyntax invocation,
        string invokedMemberName,
        ArgumentListSyntax argumentList)
    {
        var invocationLeadingTrivia = invocation.GetLeadingTrivia()
            .Where(trivia => !trivia.IsKind(SyntaxKind.EndOfLineTrivia));
        var newInvocationTrivia = new SyntaxTriviaList(
            SyntaxFactory.EndOfLine(Environment.NewLine),
            SyntaxFactory.Tab)
            .AddRange(invocationLeadingTrivia);

        return SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                invocation.WithTrailingTrivia(newInvocationTrivia),
                SyntaxFactory.IdentifierName(invokedMemberName)),
           argumentList);
    }

    private static Task<Document> ReplaceWithAddAuthorizationBuilder(Diagnostic diagnostic, SyntaxNode root, Document document, InvocationExpressionSyntax invocation)
    {
        var diagnosticTarget = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);

        return Task.FromResult(document.WithSyntaxRoot(
            root.ReplaceNode(diagnosticTarget, invocation)));
    }
}
