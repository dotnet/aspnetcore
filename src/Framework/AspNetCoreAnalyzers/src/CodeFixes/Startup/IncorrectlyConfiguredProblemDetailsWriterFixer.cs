// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.AspNetCore.Analyzers.Startup.Fixers;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class IncorrectlyConfiguredProblemDetailsWriterFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds { get; } = [DiagnosticDescriptors.IncorrectlyConfiguredProblemDetailsWriter.Id];

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

        foreach (var diagnostic in context.Diagnostics)
        {
            if (CanFixOrderOfProblemDetailsWriter(diagnostic, root, out var registerProblemDetailsWriterInvocation, out var mvcServiceInvocation))
            {
                const string title = "Fix order of ProblemDetailsWriter registration";
                context.RegisterCodeFix(
                    CodeAction.Create(
                        title,
                        cancellationToken => FixOrderOfProblemDetailsWriter(root, context.Document, registerProblemDetailsWriterInvocation, mvcServiceInvocation),
                        equivalenceKey: DiagnosticDescriptors.IncorrectlyConfiguredProblemDetailsWriter.Id),
                    diagnostic);
            }
        }
    }

    private static bool CanFixOrderOfProblemDetailsWriter(
        Diagnostic diagnostic,
        SyntaxNode root,
        [NotNullWhen(true)] out InvocationExpressionSyntax? registerProblemDetailsWriterInvocation,
        [NotNullWhen(true)] out InvocationExpressionSyntax? mvcServiceInvocation)
    {
        registerProblemDetailsWriterInvocation = null;
        mvcServiceInvocation = null;

        var registerProblemDetailsWriterInvocationTarget = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);
        if (registerProblemDetailsWriterInvocationTarget is not InvocationExpressionSyntax registerProblemDetailsWriterInvocationSyntax)
        {
            return false;
        }

        registerProblemDetailsWriterInvocation = registerProblemDetailsWriterInvocationSyntax;

        var mvcServiceInvocationLocation = diagnostic.AdditionalLocations.FirstOrDefault();
        if (mvcServiceInvocationLocation is null)
        {
            return false;
        }

        var mvcServiceInvocationTarget = root.FindNode(mvcServiceInvocationLocation.SourceSpan, getInnermostNodeForTie: true);
        if (mvcServiceInvocationTarget is not InvocationExpressionSyntax mvcServiceInvocationSyntax)
        {
            return false;
        }

        mvcServiceInvocation = mvcServiceInvocationSyntax;

        return true;
    }

    private static Task<Document> FixOrderOfProblemDetailsWriter(
        SyntaxNode root,
        Document document,
        InvocationExpressionSyntax registerProblemDetailsWriterInvocation,
        InvocationExpressionSyntax mvcServiceInvocation)
    {
        var newRoot = root.ReplaceNodes(
            new[] { registerProblemDetailsWriterInvocation, mvcServiceInvocation },
            (original, _) => original == registerProblemDetailsWriterInvocation ? mvcServiceInvocation : registerProblemDetailsWriterInvocation);

        return Task.FromResult(document.WithSyntaxRoot(newRoot));
    }
}
