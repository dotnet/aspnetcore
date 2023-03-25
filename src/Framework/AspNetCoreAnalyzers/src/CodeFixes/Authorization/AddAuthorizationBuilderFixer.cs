// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
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
                        equivalenceKey: title),
                    diagnostic);
            }
        }
    }

    private static bool CanReplaceWithAddAuthorizationBuilder(Diagnostic diagnostic, SyntaxNode root, [NotNullWhen(true)] out InvocationExpressionSyntax? invocation)
    {
        invocation = null;

        var diagnosticTarget = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);

        if (diagnosticTarget is InvocationExpressionSyntax { ArgumentList.Arguments: { } arguments, Expression: MemberAccessExpressionSyntax { Name.Identifier: { } identifierToken } memberAccessExpression } invocationExpression)
        {
            if (arguments.Count == 1
                && arguments[0].Expression is SimpleLambdaExpressionSyntax lambda)
            {
                if (lambda.Body is not BlockSyntax lambdaBody)
                {
                    return false;
                }

                var addAuthorizationBuilderMethod =
                    memberAccessExpression.ReplaceToken(identifierToken, SyntaxFactory.Identifier("AddAuthorizationBuilder"));

                invocation = SyntaxFactory.InvocationExpression(addAuthorizationBuilderMethod);

                var lambdaNodes = lambdaBody.DescendantNodes();

                foreach (var configureAction in lambdaNodes)
                {
                    if (configureAction is InvocationExpressionSyntax { ArgumentList.Arguments: { } configureArguments, Expression: MemberAccessExpressionSyntax { Name.Identifier.Text: "AddPolicy" } })
                    {
                        if (configureArguments.Count == 2)
                        {
                            invocation = SyntaxFactory.InvocationExpression(
                                SyntaxFactory.MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    invocation.WithTrailingTrivia(
                                        SyntaxFactory.EndOfLine(Environment.NewLine),
                                        SyntaxFactory.Space,
                                        SyntaxFactory.Space,
                                        SyntaxFactory.Space,
                                        SyntaxFactory.Space),
                                    SyntaxFactory.IdentifierName("AddPolicy")),
                                SyntaxFactory.ArgumentList(
                                    SyntaxFactory.SeparatedList(configureArguments)));
                        }
                    }
                    else if (configureAction is AssignmentExpressionSyntax assignmentSyntax)
                    {
                        if (assignmentSyntax.Left is MemberAccessExpressionSyntax { Name.Identifier.Text: { } assignmentTargetName })
                        {
                            if (assignmentTargetName == "DefaultPolicy")
                            {
                                invocation = SyntaxFactory.InvocationExpression(
                                   SyntaxFactory.MemberAccessExpression(
                                       SyntaxKind.SimpleMemberAccessExpression,
                                       invocation.WithTrailingTrivia(
                                           SyntaxFactory.EndOfLine(Environment.NewLine),
                                           SyntaxFactory.Space,
                                           SyntaxFactory.Space,
                                           SyntaxFactory.Space,
                                           SyntaxFactory.Space),
                                       SyntaxFactory.IdentifierName("SetDefaultPolicy")),
                                   SyntaxFactory.ArgumentList(
                                       SyntaxFactory.SingletonSeparatedList(
                                           SyntaxFactory.Argument(assignmentSyntax.Right))));
                            }
                            else if (assignmentTargetName == "FallbackPolicy")
                            {
                                invocation = SyntaxFactory.InvocationExpression(
                                   SyntaxFactory.MemberAccessExpression(
                                       SyntaxKind.SimpleMemberAccessExpression,
                                       invocation.WithTrailingTrivia(
                                           SyntaxFactory.EndOfLine(Environment.NewLine),
                                           SyntaxFactory.Space,
                                           SyntaxFactory.Space,
                                           SyntaxFactory.Space,
                                           SyntaxFactory.Space),
                                       SyntaxFactory.IdentifierName("SetFallbackPolicy")),
                                   SyntaxFactory.ArgumentList(
                                       SyntaxFactory.SingletonSeparatedList(
                                           SyntaxFactory.Argument(assignmentSyntax.Right))));
                            }
                        }
                    }
                }

                return true;
            }
        }

        return false;
    }

    private static Task<Document> ReplaceWithAddAuthorizationBuilder(Diagnostic diagnostic, SyntaxNode root, Document document, InvocationExpressionSyntax invocation)
    {
        var diagnosticTarget = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);

        return Task.FromResult(document.WithSyntaxRoot(
            root.ReplaceNode(diagnosticTarget, invocation)));
    }
}
