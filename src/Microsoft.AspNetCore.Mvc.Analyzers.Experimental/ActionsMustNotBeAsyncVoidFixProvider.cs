// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Editing;

namespace Microsoft.AspNetCore.Mvc.Analyzers
{
    [ExportCodeFixProvider(LanguageNames.CSharp)]
    [Shared]
    public class ActionsMustNotBeAsyncVoidFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(DiagnosticDescriptors.MVC7003_ActionsMustNotBeAsyncVoid.Id);

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            if (context.Diagnostics.Length == 0)
            {
                return;
            }

            if (!context.Diagnostics[0].Properties.TryGetValue(ActionsMustNotBeAsyncVoidAnalyzer.ReturnTypeKey, out var returnTypeName))
            {
                return;
            }

            var rootNode = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            const string title = "Fix async void usage.";
            context.RegisterCodeFix(
                CodeAction.Create(
                    title,
                    createChangedDocument: CreateChangedDocumentAsync,
                    equivalenceKey: title),
                context.Diagnostics);

            async Task<Document> CreateChangedDocumentAsync(CancellationToken cancellationToken)
            {
                var returnTypeSyntax = rootNode.FindNode(context.Span);

                var editor = await DocumentEditor.CreateAsync(context.Document, cancellationToken).ConfigureAwait(false);
                editor.ReplaceNode(returnTypeSyntax, SyntaxFactory.IdentifierName(returnTypeName));

                return editor.GetChangedDocument();
            }
        }
    }
}
