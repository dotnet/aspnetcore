// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.App.Analyzers.Infrastructure;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.AspNetCore.Analyzers.Http.Fixers;

using WellKnownType = WellKnownTypeData.WellKnownType;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class HeaderDictionaryAddFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(DiagnosticDescriptors.DoNotUseIHeaderDictionaryAdd.Id);

    public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
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

        var wellKnownTypes = WellKnownTypes.GetOrCreate(semanticModel.Compilation);

        foreach (var diagnostic in context.Diagnostics)
        {
            if (CanReplaceWithAppend(diagnostic, root, out var invocation))
            {
                var appendTitle = "Use 'IHeaderDictionary.Append'";
                context.RegisterCodeFix(
                    CodeAction.Create(appendTitle,
                        cancellationToken => ReplaceWithAppend(diagnostic, wellKnownTypes, root, context.Document, invocation),
                        equivalenceKey: appendTitle),
                    diagnostic);
            }

            if (CanReplaceWithIndexer(diagnostic, root, out var assignment))
            {
                var indexerTitle = "Use indexer";
                context.RegisterCodeFix(
                    CodeAction.Create(indexerTitle,
                        cancellationToken => ReplaceWithIndexer(diagnostic, root, context.Document, assignment),
                        equivalenceKey: indexerTitle),
                    diagnostic);
            }
        }
    }

    private static Task<Document> ReplaceWithAppend(Diagnostic diagnostic, WellKnownTypes wellKnownTypes, SyntaxNode root, Document document, InvocationExpressionSyntax invocation)
    {
        var diagnosticTarget = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);

        var annotation = new SyntaxAnnotation("SymbolId", DocumentationCommentId.CreateReferenceId(wellKnownTypes.Get(WellKnownType.Microsoft_AspNetCore_Http_HeaderDictionaryExtensions)));

        return Task.FromResult(document.WithSyntaxRoot(
            root.ReplaceNode(diagnosticTarget, invocation.WithAdditionalAnnotations(Simplifier.AddImportsAnnotation, annotation))));
    }

    private static bool CanReplaceWithAppend(Diagnostic diagnostic, SyntaxNode root, [NotNullWhen(true)] out InvocationExpressionSyntax? invocation)
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

    private static Task<Document> ReplaceWithIndexer(Diagnostic diagnostic, SyntaxNode root, Document document, AssignmentExpressionSyntax assignment)
    {
        var diagnosticTarget = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);

        return Task.FromResult(document.WithSyntaxRoot(root.ReplaceNode(diagnosticTarget, assignment)));
    }

    private static bool CanReplaceWithIndexer(Diagnostic diagnostic, SyntaxNode root, [NotNullWhen(true)] out AssignmentExpressionSyntax? assignment)
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
