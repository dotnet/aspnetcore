// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.AspNetCore.Components.Analyzers
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ComponentClosureOverLoopVariablesCodeFixProvider)), Shared]
    public class ComponentClosureOverLoopVariablesCodeFixProvider : CodeFixProvider
    {
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.ComponentClosureOverLoopVariables_FixTitle), Resources.ResourceManager, typeof(Resources));

        public override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(DiagnosticDescriptors.ClosureOverLoopVariables.Id);

        public sealed override FixAllProvider GetFixAllProvider()
            => null;

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            // Find the identifier declaration identified by the diagnostic.
            var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<IdentifierNameSyntax>().First();

            // Register a code action that will invoke the fix.
            var title = Title.ToString();
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: title,
                    createChangedDocument: c => GetTransformedDocumentAsync(context.Document, root, declaration),
                    equivalenceKey: title),
                diagnostic);
        }

        private Task<Document> GetTransformedDocumentAsync(
            Document document,
            SyntaxNode root,
            IdentifierNameSyntax declarationNode)
        {

            var loopVariableName = declarationNode.Identifier.Text;

            var loopStatement = declarationNode
                .Parent
                .AncestorsAndSelf()
                .Where(node => node.IsKind(SyntaxKind.ForStatement))
                .First() as ForStatementSyntax;

            var existingVariable = loopStatement
                .Statement
                .ChildNodes()
                .OfType<LocalDeclarationStatementSyntax>()
                .Where(node
                    => node
                        .DescendantNodes()
                        .OfType<EqualsValueClauseSyntax>()
                        .Any(dcnode => (dcnode.Value as IdentifierNameSyntax)?.Identifier.Text == loopVariableName)
                    )
                .FirstOrDefault();

            var existingName = existingVariable?.Declaration?.Variables.FirstOrDefault()?.Identifier.Text;

            var localIdentifier = SyntaxFactory
            .IdentifierName(existingName ?? $"lcl_{loopVariableName.ToUpper()}")
            .WithTriviaFrom(declarationNode);

            // Track the nodes we are modifying
            var tracker = root.TrackNodes(declarationNode, loopStatement);

            // Replace the loop variable with the identifier of the local variable
            declarationNode = tracker.GetCurrentNode(declarationNode);
            tracker = tracker.ReplaceNode(declarationNode, localIdentifier);

            if (existingName is null)
            {
                loopStatement = tracker.GetCurrentNode(loopStatement);

                var localVariableDeclaration = $"var {localIdentifier.Identifier.Text}";

                // Introduce a new locally scoped variable
                if (loopStatement.Statement.Kind() != SyntaxKind.Block)
                {
                    var newStatement = SyntaxFactory
                        .ParseStatement($"{localVariableDeclaration} = {loopVariableName};")
                        .WithTriviaFrom(loopStatement.Statement);

                    var blockContent = SyntaxFactory.Block(loopStatement.Statement)
                        .WithTriviaFrom(loopStatement);

                    blockContent = blockContent
                        .InsertNodesBefore(blockContent.Statements.First(), new SyntaxNode[] { newStatement });

                    var newLoopStatement = loopStatement.ReplaceNode(loopStatement.Statement, blockContent);

                    tracker = tracker.ReplaceNode(loopStatement, newLoopStatement);
                }
                else
                {
                    SyntaxList<StatementSyntax> blockStatements = ((BlockSyntax)loopStatement.Statement).Statements;

                    var newStatement = SyntaxFactory
                        .ParseStatement($"{localVariableDeclaration} = {loopVariableName};")
                        .WithTriviaFrom(blockStatements.First());

                    var blockContent = SyntaxFactory.Block(blockStatements)
                        .WithTriviaFrom(loopStatement.Statement);

                    blockContent = blockContent
                        .InsertNodesBefore(blockContent.Statements.First(), new SyntaxNode[] { newStatement });

                    var newLoopStatement = loopStatement.ReplaceNode(loopStatement.Statement, blockContent);

                    tracker = tracker.ReplaceNode(loopStatement, newLoopStatement);
                }
            }
            return Task.FromResult(document.WithSyntaxRoot(tracker));
        }
    }
}
