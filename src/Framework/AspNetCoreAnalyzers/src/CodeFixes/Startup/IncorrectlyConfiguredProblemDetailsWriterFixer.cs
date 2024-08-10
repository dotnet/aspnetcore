// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

namespace Microsoft.AspNetCore.Analyzers.Startup.Fixers;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class IncorrectlyConfiguredProblemDetailsWriterFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds { get; } = [DiagnosticDescriptors.IncorrectlyConfiguredProblemDetailsWriter.Id];

    public sealed override FixAllProvider? GetFixAllProvider()
    {
        return FixAllProvider.Create(async (context, document, diagnostics) =>
        {
            return await FixOrderOfAllProblemDetailsWriter(document, diagnostics, context.CancellationToken).ConfigureAwait(false);
        });
    }

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root == null)
        {
            return;
        }

        foreach (var diagnostic in context.Diagnostics)
        {
            if (CanFixOrderOfProblemDetailsWriter(diagnostic, root, out var problemDetailsWriter, out var mvcServiceCollectionExtension))
            {
                const string title = "Fix order of ProblemDetailsWriter registration";

                async Task<Document> CreateChangedDocument(CancellationToken cancellationToken) =>
                    await FixOrderOfProblemDetailsWriter(
                        context.Document,
                        problemDetailsWriter,
                        mvcServiceCollectionExtension,
                        cancellationToken).ConfigureAwait(false);

                var codeAction = CodeAction.Create(
                    title,
                    CreateChangedDocument,
                    equivalenceKey: DiagnosticDescriptors.IncorrectlyConfiguredProblemDetailsWriter.Id);

                context.RegisterCodeFix(codeAction, diagnostic);
            }
        }
    }

    private static bool CanFixOrderOfProblemDetailsWriter(
        Diagnostic diagnostic,
        SyntaxNode root,
        [NotNullWhen(true)] out ExpressionStatementSyntax? problemDetailsWriterStatement,
        [NotNullWhen(true)] out ExpressionStatementSyntax? mvcServiceCollectionExtensionStatement)
    {
        problemDetailsWriterStatement = null;
        mvcServiceCollectionExtensionStatement = null;

        Debug.Assert(diagnostic.AdditionalLocations.Count == 1,
            "Expected exactly one additional location for the MvcServiceCollectionExtension.");

        return diagnostic.AdditionalLocations.Count == 1 &&
            // Ensure that the ProblemDetailsWriter registration appears after the MvcServiceCollectionExtension in source.
            diagnostic.Location.SourceSpan.CompareTo(diagnostic.AdditionalLocations[0].SourceSpan) > 0 &&
            TryGetInvocationExpressionStatement(diagnostic.Location, root, out problemDetailsWriterStatement) &&
            // Exclude ProblemDetailsWriter registrations that may be part of an invocation chain to avoid moving unrelated code.
            !IsPotentiallyPartOfInvocationChain(problemDetailsWriterStatement) &&
            TryGetInvocationExpressionStatement(diagnostic.AdditionalLocations[0], root, out mvcServiceCollectionExtensionStatement);
    }

    private static async Task<Document> FixOrderOfProblemDetailsWriter(
        Document document,
        ExpressionStatementSyntax problemDetailsWriterExpression,
        ExpressionStatementSyntax mvcServiceCollectionExtensionExpression,
        CancellationToken cancellationToken)
    {
        var groupedStatements = new Dictionary<ExpressionStatementSyntax, IList<ExpressionStatementSyntax>>
        {
            [mvcServiceCollectionExtensionExpression] = [problemDetailsWriterExpression],
        };

        return await MoveProblemDetailsWritersBeforeMvcExtensions(document, groupedStatements, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<Document> FixOrderOfAllProblemDetailsWriter(
        Document document,
        ImmutableArray<Diagnostic> diagnostics,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null)
        {
            return document;
        }

        var groupedStatements = new Dictionary<ExpressionStatementSyntax, IList<ExpressionStatementSyntax>>();

        foreach (var diagnostic in diagnostics)
        {
            if (!CanFixOrderOfProblemDetailsWriter(diagnostic, root, out var problemDetailsWriterStatement, out var mvcServiceCollectionExtensionStatement))
            {
                continue;
            }

            if (groupedStatements.TryGetValue(mvcServiceCollectionExtensionStatement, out var problemDetailsWriterStatements))
            {
                problemDetailsWriterStatements.Add(problemDetailsWriterStatement);
            }
            else
            {
                groupedStatements[mvcServiceCollectionExtensionStatement] = [problemDetailsWriterStatement];
            }
        }

        if (groupedStatements.Count == 0)
        {
            return document;
        }

        // Maintain the relative source order of the ProblemDetailsWriter registrations. 
        var comparer = Comparer<ExpressionStatementSyntax>.Create((a, b) => a.Span.CompareTo(b.Span));

        foreach (var group in groupedStatements)
        {
            groupedStatements[group.Key] = [.. group.Value.OrderBy(x => x, comparer)];
        }

        return await MoveProblemDetailsWritersBeforeMvcExtensions(document, groupedStatements, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<Document> MoveProblemDetailsWritersBeforeMvcExtensions(
        Document document,
        IDictionary<ExpressionStatementSyntax, IList<ExpressionStatementSyntax>> groupedStatements,
        CancellationToken cancellationToken)
    {
        var documentEditor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);

        foreach (var group in groupedStatements)
        {
            var mvcServiceExtensionExpression = group.Key;
            var registerProblemDetailsWriterExpressions = group.Value;

            foreach (var registerProblemDetailsWriterExpression in registerProblemDetailsWriterExpressions)
            {
                documentEditor.RemoveNode(registerProblemDetailsWriterExpression, SyntaxRemoveOptions.KeepNoTrivia);
            }

            documentEditor.InsertBefore(mvcServiceExtensionExpression, registerProblemDetailsWriterExpressions);
        }

        return documentEditor.GetChangedDocument();
    }

    private static bool TryGetInvocationExpressionStatement(
        Location location,
        SyntaxNode root,
        [NotNullWhen(true)] out ExpressionStatementSyntax? expressionStatement)
    {
        expressionStatement = null;

        var node = root.FindNode(location.SourceSpan, getInnermostNodeForTie: true);

        if (node is not InvocationExpressionSyntax)
        {
            return false;
        }

        var parentNode = node.Parent;

        while (parentNode != null)
        {
            if (parentNode is ExpressionStatementSyntax expressionStatementSyntax)
            {
                expressionStatement = expressionStatementSyntax;
                return true;
            }

            if (parentNode is not MemberAccessExpressionSyntax
                && parentNode is not InvocationExpressionSyntax)
            {
                break;
            }

            parentNode = parentNode.Parent;
        }

        return false;
    }

    private static bool IsPotentiallyPartOfInvocationChain(ExpressionStatementSyntax expressionStatement)
    {
        if (expressionStatement.Expression is InvocationExpressionSyntax invocationExpressionSyntax &&
            invocationExpressionSyntax.Expression is MemberAccessExpressionSyntax memberAccessExpression &&
            memberAccessExpression.Expression is IdentifierNameSyntax)
        {
            return false;
        }

        return true;
    }
}
