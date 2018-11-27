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
    public class ApiActionsShouldUseActionResultOfTCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(DiagnosticDescriptors.MVC7002_ApiActionsShouldReturnActionResultOf.Id);

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var rootNode = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            foreach (var diagnostic in context.Diagnostics)
            {
                if (diagnostic.Properties.TryGetValue("ReturnType", out var returnTypeName))
                {

                    var title = $"Make return type {returnTypeName}";
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            title,
                            createChangedDocument: cancellationToken => CreateChangedDocumentAsync(returnTypeName, cancellationToken),
                            equivalenceKey: title),
                        context.Diagnostics);
                }
            }

            async Task<Document> CreateChangedDocumentAsync(string returnTypeName, CancellationToken cancellationToken)
            {
                var returnType = rootNode.FindNode(context.Span);

                var editor = await DocumentEditor.CreateAsync(context.Document, cancellationToken).ConfigureAwait(false);
                editor.ReplaceNode(returnType, SyntaxFactory.IdentifierName(returnTypeName));

                return editor.GetChangedDocument();
            }
        }
    }
}
