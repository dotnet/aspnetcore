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
            var appendTitle = "Use 'IHeaderDictionary.Append'";
            context.RegisterCodeFix(
                CodeAction.Create(appendTitle,
                    cancellationToken => ReplaceAddWithAppendAsync(diagnostic, context.Document, cancellationToken),
                    equivalenceKey: appendTitle),
                diagnostic);

            var indexerTitle = "Use indexer";
            context.RegisterCodeFix(
                CodeAction.Create(indexerTitle,
                    cancellationToken => ReplaceAddWithIndexerAsync(diagnostic, context.Document, cancellationToken),
                    equivalenceKey: indexerTitle),
                diagnostic);
        }

        return Task.CompletedTask;
    }

    private static async Task<Document> ReplaceAddWithAppendAsync(Diagnostic diagnostic, Document document, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is not CompilationUnitSyntax compilationUnitSyntax)
        {
            return document;
        }

        var invocation = compilationUnitSyntax.FindNode(diagnostic.Location.SourceSpan);

        if (invocation is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Name.Identifier: { } identifierToken } })
        {
            compilationUnitSyntax = compilationUnitSyntax.ReplaceToken(identifierToken, SyntaxFactory.Identifier("Append"));

            // IHeaderDictionary.Append is defined as an extension method on Microsoft.AspNetCore.Http.HeaderDictionaryExtensions.
            // We'll need to add the required using directive, when not already present.
            compilationUnitSyntax = AddRequiredUsingDirectiveForAppend(compilationUnitSyntax);

            return document.WithSyntaxRoot(compilationUnitSyntax);
        }

        return document;
    }

    private static CompilationUnitSyntax AddRequiredUsingDirectiveForAppend(CompilationUnitSyntax compilationUnitSyntax)
    {
        var usingDirectives = compilationUnitSyntax.Usings;

        var includesRequiredUsingDirective = false;
        var insertionIndex = 0;

        for (var i = 0; i < usingDirectives.Count; i++)
        {
            var namespaceName = usingDirectives[i].Name.ToString();

            // Always insert the new using directive after any 'System' using directives.
            if (namespaceName.StartsWith("System", StringComparison.Ordinal))
            {
                insertionIndex = i + 1;
                continue;
            }

            var result = string.Compare("Microsoft.AspNetCore.Http", namespaceName, StringComparison.Ordinal);

            if (result == 0)
            {
                includesRequiredUsingDirective = true;
                break;
            }

            if (result < 0)
            {
                insertionIndex = i;
                break;
            }
        }

        if (includesRequiredUsingDirective)
        {
            return compilationUnitSyntax;
        }

        var requiredUsingDirective =
            SyntaxFactory.UsingDirective(
                SyntaxFactory.QualifiedName(
                    SyntaxFactory.QualifiedName(
                        SyntaxFactory.IdentifierName("Microsoft"),
                        SyntaxFactory.IdentifierName("AspNetCore")),
                    SyntaxFactory.IdentifierName("Http")));

        return compilationUnitSyntax.WithUsings(
            usingDirectives.Insert(insertionIndex, requiredUsingDirective));
    }

    private static async Task<Document> ReplaceAddWithIndexerAsync(Diagnostic diagnostic, Document document, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null)
        {
            return document;
        }

        var invocation = root.FindNode(diagnostic.Location.SourceSpan);

        if (invocation is InvocationExpressionSyntax
            {
                Expression: MemberAccessExpressionSyntax memberAccessExpression,
                ArgumentList.Arguments: { Count: 2 } arguments
            })
        {
            var assignmentExpression =
                SyntaxFactory.AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    SyntaxFactory.ElementAccessExpression(
                        memberAccessExpression.Expression,
                        SyntaxFactory.BracketedArgumentList(
                            SyntaxFactory.SeparatedList(new[] { arguments[0] }))),
                    arguments[1].Expression);

            return document.WithSyntaxRoot(root.ReplaceNode(invocation, assignmentExpression));
        }

        return document;
    }
}
