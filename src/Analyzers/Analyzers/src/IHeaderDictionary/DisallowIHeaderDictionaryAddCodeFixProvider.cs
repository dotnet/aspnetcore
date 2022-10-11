// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.AspNetCore.Analyzers.IHeaderDictionary;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class DisallowIHeaderDictionaryAddCodeFixProvider : CodeFixProvider
{
    public sealed override ImmutableArray<string> FixableDiagnosticIds
        => ImmutableArray.Create(DisallowIHeaderDictionaryAddAnalyzer.Diagnostics.DisallowIHeaderDictionaryAdd.Id);

    public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        if (context.Diagnostics.Length != 1)
        {
            return Task.CompletedTask;
        }

        var diagnostic = context.Diagnostics[0];
        if (diagnostic.Id != DisallowIHeaderDictionaryAddAnalyzer.Diagnostics.DisallowIHeaderDictionaryAdd.Id)
        {
            return Task.CompletedTask;
        }

        context.RegisterCodeFix(new UseAppendCodeAction(context.Document, context.Span), diagnostic);
        context.RegisterCodeFix(new UseIndexerCodeAction(context.Document, context.Span), diagnostic);

        return Task.CompletedTask;
    }

    private sealed class UseAppendCodeAction : CodeAction
    {
        private readonly Document _document;
        private readonly TextSpan _invocationSpan;

        public UseAppendCodeAction(Document document, TextSpan invocationSpan)
        {
            _document = document;
            _invocationSpan = invocationSpan;
        }

        public override string EquivalenceKey => $"{DisallowIHeaderDictionaryAddAnalyzer.Diagnostics.DisallowIHeaderDictionaryAdd.Id}.Append";

        public override string Title => "Use Append";

        protected override async Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
        {
            var rootNode = await _document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var editor = await DocumentEditor.CreateAsync(_document, cancellationToken).ConfigureAwait(false);

            var syntaxTree = await _document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var compilationUnitRoot = syntaxTree.GetCompilationUnitRoot(cancellationToken);
            AddRequiredUsingDirectives(editor, compilationUnitRoot);

            var invocationExpressionSyntax = (InvocationExpressionSyntax)rootNode!.FindNode(_invocationSpan);
            var addMethodNode = GetAddMethodIdentiferNode(invocationExpressionSyntax);

            var appendMethodNode = addMethodNode.ReplaceToken(
                addMethodNode.Identifier,
                SyntaxFactory.Identifier(
                    SymbolNames.IHeaderDictionary.AppendMethodName));

            editor.ReplaceNode(addMethodNode, appendMethodNode);

            return editor.GetChangedDocument();
        }

        private static IdentifierNameSyntax GetAddMethodIdentiferNode(InvocationExpressionSyntax invocationExpressionSyntax)
        {
            return invocationExpressionSyntax.DescendantNodes()
                .OfType<MemberAccessExpressionSyntax>()
                .First()
                .DescendantNodes()
                .OfType<IdentifierNameSyntax>()
                .First(x => string.Equals(x.Identifier.ValueText, SymbolNames.IHeaderDictionary.AddMethodName, StringComparison.Ordinal));
        }

        private static void AddRequiredUsingDirectives(DocumentEditor editor, CompilationUnitSyntax compilationUnitSyntax)
        {
            if (!compilationUnitSyntax.Usings.Any(u => string.Equals(u.Name.ToString(), "Microsoft.AspNetCore.Http", StringComparison.Ordinal)))
            {
                // IHeaderDictionary.Append is defined as an extension method on Microsoft.AspNetCore.Http.HeaderDictionaryExtensions.
                var usingDirectiveSyntax = SyntaxFactory.UsingDirective(SyntaxFactory.IdentifierName("Microsoft.AspNetCore.Http"));

                var newCompilationUnitSyntax =
                    compilationUnitSyntax.WithUsings(new SyntaxList<UsingDirectiveSyntax>(usingDirectiveSyntax));

                editor.ReplaceNode(compilationUnitSyntax, newCompilationUnitSyntax);
            }
        }
    }

    private sealed class UseIndexerCodeAction : CodeAction
    {
        private readonly Document _document;
        private readonly TextSpan _invocationSpan;

        public UseIndexerCodeAction(Document document, TextSpan invocationSpan)
        {
            _document = document;
            _invocationSpan = invocationSpan;
        }

        public override string EquivalenceKey => $"{DisallowIHeaderDictionaryAddAnalyzer.Diagnostics.DisallowIHeaderDictionaryAdd.Id}.Indexer";

        public override string Title => "Use Indexer";

        protected override async Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
        {
            var rootNode = await _document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var editor = await DocumentEditor.CreateAsync(_document, cancellationToken).ConfigureAwait(false);

            var invocationExpressionSyntax = (InvocationExpressionSyntax)rootNode!.FindNode(_invocationSpan);

            var headerDictionaryNode = GetHeaderDictionaryNodes(invocationExpressionSyntax);
            var (keyArgumentNode, valueArgumentNode) = GetArgumentNodes(invocationExpressionSyntax);

            var targetNode =
                SyntaxFactory.ElementAccessExpression(
                    headerDictionaryNode,
                    SyntaxFactory.BracketedArgumentList(
                        SyntaxFactory.SeparatedList(new[] { keyArgumentNode })));

            var indexerAssignmentExpression =
                SyntaxFactory.AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    targetNode,
                    valueArgumentNode.Expression); ;

            editor.ReplaceNode(invocationExpressionSyntax, indexerAssignmentExpression);

            return editor.GetChangedDocument();
        }

        private static (ArgumentSyntax keyArgument, ArgumentSyntax valueArgument) GetArgumentNodes(
            InvocationExpressionSyntax invocationExpressionSyntax)
        {
            var arguments = invocationExpressionSyntax.DescendantNodes()
                .OfType<ArgumentListSyntax>()
                .First()
                .Arguments;

            return (arguments[0], arguments[1]);
        }

        private static ExpressionSyntax GetHeaderDictionaryNodes(InvocationExpressionSyntax invocationExpressionSyntax)
        {
            return invocationExpressionSyntax.DescendantNodes()
                .OfType<MemberAccessExpressionSyntax>()
                .First()
                .Expression;
        }
    }
}
