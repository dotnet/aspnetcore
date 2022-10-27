// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.AspNetCore.Analyzers.Http.Fixers;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class HeaderDictionaryAddFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(DiagnosticDescriptors.DoNotUseIHeaderDictionaryAdd.Id);

    public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        foreach (var diagnostic in context.Diagnostics)
        {
            context.Document.TryGetSyntaxRoot(out var root);

            if (CanReplaceWithAppend(diagnostic, root, out var invocation))
            {
                var appendTitle = "Use 'IHeaderDictionary.Append'";
                context.RegisterCodeFix(
                    CodeAction.Create(appendTitle,
                        cancellationToken => ReplaceWithAppend(diagnostic, context.Document, invocation, cancellationToken),
                        equivalenceKey: appendTitle),
                    diagnostic);
            }

            if (CanReplaceWithIndexer(diagnostic, root, out var assignment))
            {
                var indexerTitle = "Use indexer";
                context.RegisterCodeFix(
                    CodeAction.Create(indexerTitle,
                        cancellationToken => ReplaceWithIndexer(diagnostic, context.Document, assignment, cancellationToken),
                        equivalenceKey: indexerTitle),
                    diagnostic);
            }
        }

        return Task.CompletedTask;
    }

    private static async Task<Document> ReplaceWithAppend(Diagnostic diagnostic, Document document, InvocationExpressionSyntax invocation, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        var diagnosticTarget = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);

        // IHeaderDictionary.Append is defined as an extension method on Microsoft.AspNetCore.Http.HeaderDictionaryExtensions.
        // We'll need to add the required using directive, when not already present.
        var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var headerDictionaryExtensionsSymbol = model.Compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Http.HeaderDictionaryExtensions");
        var annotation = new SyntaxAnnotation("SymbolId", DocumentationCommentId.CreateReferenceId(headerDictionaryExtensionsSymbol));

        return document.WithSyntaxRoot(
            root.ReplaceNode(diagnosticTarget, invocation.WithAdditionalAnnotations(Simplifier.Annotation, Simplifier.AddImportsAnnotation, annotation)));
    }

    private static bool CanReplaceWithAppend(Diagnostic diagnostic, SyntaxNode root, out InvocationExpressionSyntax invocation)
    {
        invocation = null;

        if (root is not CompilationUnitSyntax)
        {
            return false;
        }

        var diagnosticTarget = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);

        if (diagnosticTarget is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Name.Identifier: { } identifierToken } } invocationExpression)
        {
            invocation = invocationExpression.ReplaceToken(identifierToken, SyntaxFactory.Identifier("Append"));

            return true;
        }

        return false;
    }

    private static async Task<Document> ReplaceWithIndexer(Diagnostic diagnostic, Document document, AssignmentExpressionSyntax assignment, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        var diagnosticTarget = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);

        return document.WithSyntaxRoot(root.ReplaceNode(diagnosticTarget, assignment));
    }

    private static bool CanReplaceWithIndexer(Diagnostic diagnostic, SyntaxNode root, out AssignmentExpressionSyntax assignment)
    {
        assignment = null;

        if (root is null)
        {
            return false;
        }

        var diagnosticTarget = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);

        if (diagnosticTarget is InvocationExpressionSyntax
            {
                Expression: MemberAccessExpressionSyntax memberAccessExpression,
                ArgumentList.Arguments: { Count: 2 } arguments
            })
        {
            assignment =
                SyntaxFactory.AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    SyntaxFactory.ElementAccessExpression(
                        memberAccessExpression.Expression,
                        SyntaxFactory.BracketedArgumentList(
                            SyntaxFactory.SeparatedList(new[] { arguments[0] }))),
                    arguments[1].Expression);

            return true;
        }

        return false;
    }
}
