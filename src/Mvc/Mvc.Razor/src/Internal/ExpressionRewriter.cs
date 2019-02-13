// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.AspNetCore.Mvc.Razor.Internal
{
    /// <summary>
    /// An expression rewriter which can hoist a simple expression lambda into a private field.
    /// </summary>
    public class ExpressionRewriter : CSharpSyntaxRewriter
    {
        private static readonly string FieldNameTemplate = "__h{0}";

        public ExpressionRewriter(SemanticModel semanticModel)
        {
            SemanticModel = semanticModel;

            Expressions = new List<KeyValuePair<SimpleLambdaExpressionSyntax, IdentifierNameSyntax>>();
        }

        // We only want to rewrite expressions for the top-level class definition.
        private bool IsInsideClass { get; set; }

        private SemanticModel SemanticModel { get; }

        private List<KeyValuePair<SimpleLambdaExpressionSyntax, IdentifierNameSyntax>> Expressions { get; }

        public static CSharpCompilation Rewrite(CSharpCompilation compilation)
        {
            var rewrittenTrees = new List<SyntaxTree>();
            foreach (var tree in compilation.SyntaxTrees)
            {
                var semanticModel = compilation.GetSemanticModel(tree, ignoreAccessibility: true);
                var rewriter = new ExpressionRewriter(semanticModel);

                var rewrittenTree = tree.WithRootAndOptions(rewriter.Visit(tree.GetRoot()), tree.Options);
                rewrittenTrees.Add(rewrittenTree);
            }

            return compilation.RemoveAllSyntaxTrees().AddSyntaxTrees(rewrittenTrees);
        }

        public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            if (IsInsideClass)
            {
                // Avoid recursing into nested classes.
                return node;
            }

            Expressions.Clear();

            IsInsideClass = true;

            // Call base first to visit all the children and populate Expressions.
            var classDeclaration = (ClassDeclarationSyntax)base.VisitClassDeclaration(node);

            IsInsideClass = false;

            var memberDeclarations = new List<MemberDeclarationSyntax>();
            foreach (var kvp in Expressions)
            {
                var expression = kvp.Key;
                var memberName = kvp.Value.GetFirstToken();

                var expressionType = SemanticModel.GetTypeInfo(expression).ConvertedType;
                var declaration = SyntaxFactory.FieldDeclaration(
                    SyntaxFactory.List<AttributeListSyntax>(),
                    SyntaxFactory.TokenList(
                        SyntaxFactory.Token(SyntaxKind.PrivateKeyword),
                        SyntaxFactory.Token(SyntaxKind.StaticKeyword),
                        SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword)),
                    SyntaxFactory.VariableDeclaration(
                        SyntaxFactory.ParseTypeName(expressionType.ToDisplayString(
                            SymbolDisplayFormat.FullyQualifiedFormat)),
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.VariableDeclarator(
                                memberName,
                                SyntaxFactory.BracketedArgumentList(),
                                SyntaxFactory.EqualsValueClause(expression)))))
                    .WithTriviaFrom(expression);
                memberDeclarations.Add(declaration);
            }

            return classDeclaration.AddMembers(memberDeclarations.ToArray());
        }

        public override SyntaxNode VisitSimpleLambdaExpression(SimpleLambdaExpressionSyntax node)
        {
            Debug.Assert(IsInsideClass);

            // If this lambda is an Expression and is suitable for hoisting, we rewrite this into a field access.
            //
            //  Before:
            //      public Task ExecuteAsync(...)
            //      {
            //          ...
            //          Html.EditorFor(m => m.Price);
            //          ...
            //      }
            //
            //
            //  After:
            //      private static readonly Expression<Func<Product, decimal>> __h0 = m => m.Price;
            //      public Task ExecuteAsync(...)
            //      {
            //          ...
            //          Html.EditorFor(__h0);
            //          ...
            //      }
            //
            var type = SemanticModel.GetTypeInfo(node);

            // Due to an anomaly where Roslyn (depending on code sample) may finish compilation without diagnostic
            // errors (this code path does not execute when diagnostic errors are present) we need to validate that
            // the ConvertedType was determined/is not null.
            if (type.ConvertedType == null ||
                type.ConvertedType.Name != typeof(Expression).Name &&
                type.ConvertedType.ContainingNamespace.Name != typeof(Expression).Namespace)
            {
                return node;
            }

            if (!node.Parent.IsKind(SyntaxKind.Argument))
            {
                return node;
            }

            var parameter = node.Parameter;
            if (IsValidForHoisting(parameter, node.Body))
            {
                // Replace with a MemberAccess
                var memberName = string.Format(FieldNameTemplate, Expressions.Count);
                var memberAccess = PadMemberAccess(node, SyntaxFactory.IdentifierName(memberName));
                Expressions.Add(new KeyValuePair<SimpleLambdaExpressionSyntax, IdentifierNameSyntax>(node, memberAccess));
                return memberAccess;
            }

            return node;
        }

        private static IdentifierNameSyntax PadMemberAccess(
            SimpleLambdaExpressionSyntax node,
            IdentifierNameSyntax memberAccess)
        {
            var charactersToExclude = memberAccess.Identifier.Text.Length;
            var triviaList = new SyntaxTriviaList();

            // Go through each token and
            // 1. Append leading trivia
            // 2. Append the same number of whitespace as the length of the token text
            // 3. Append trailing trivia
            foreach (var token in node.DescendantTokens())
            {
                if (token.HasLeadingTrivia)
                {
                    triviaList = triviaList.AddRange(token.LeadingTrivia);
                }

                // Need to exclude the length of the member name from the padding.
                var padding = token.Text.Length;
                if (padding > charactersToExclude)
                {
                    padding -= charactersToExclude;
                    charactersToExclude = 0;
                }
                else
                {
                    charactersToExclude -= padding;
                    padding = 0;
                }

                if (padding > 0)
                {
                    triviaList = triviaList.Add(SyntaxFactory.Whitespace(new string(' ', padding)));
                }

                if (token.HasTrailingTrivia)
                {
                    triviaList = triviaList.AddRange(token.TrailingTrivia);
                }
            }

            return memberAccess
                .WithLeadingTrivia(node.GetLeadingTrivia())
                .WithTrailingTrivia(triviaList);
        }

        private static bool IsValidForHoisting(ParameterSyntax parameter, CSharpSyntaxNode node)
        {
            if (node.IsKind(SyntaxKind.IdentifierName))
            {
                var identifier = (IdentifierNameSyntax)node;
                if (identifier.Identifier.Text == parameter.Identifier.Text)
                {
                    return true;
                }
            }
            else if (node.IsKind(SyntaxKind.SimpleMemberAccessExpression))
            {
                var memberAccess = (MemberAccessExpressionSyntax)node;
                var lhs = memberAccess.Expression;
                return IsValidForHoisting(parameter, lhs);
            }

            return false;
        }
    }
}
