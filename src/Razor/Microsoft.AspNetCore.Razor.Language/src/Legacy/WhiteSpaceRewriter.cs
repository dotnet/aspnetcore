// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using Microsoft.AspNetCore.Razor.Language.Syntax;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    internal class WhitespaceRewriter : SyntaxRewriter
    {
        public override SyntaxNode Visit(SyntaxNode node)
        {
            if (node == null)
            {
                return base.Visit(node);
            }

            var children = node.ChildNodes();
            for (var i = 0; i < children.Count; i++)
            {
                var child = children[i];
                if (child is CSharpCodeBlockSyntax codeBlock &&
                    TryRewriteWhitespace(codeBlock, out var rewritten, out var whitespaceLiteral))
                {
                    // Replace the existing code block with the whitespace literal
                    // followed by the rewritten code block (with the code whitespace removed).
                    node = node.ReplaceNode(codeBlock, new SyntaxNode[] { whitespaceLiteral, rewritten });

                    // Since we replaced node, its children are different. Update our collection.
                    children = node.ChildNodes();
                }
            }

            return base.Visit(node);
        }

        private bool TryRewriteWhitespace(CSharpCodeBlockSyntax codeBlock, out CSharpCodeBlockSyntax rewritten, out SyntaxNode whitespaceLiteral)
        {
            // Rewrite any whitespace represented as code at the start of a line preceding an expression block.
            // We want it to be rendered as Markup.

            rewritten = null;
            whitespaceLiteral = null;
            var children = codeBlock.ChildNodes();
            if (children.Count < 2)
            {
                return false;
            }

            if (children[0] is CSharpStatementLiteralSyntax literal &&
                (children[1] is CSharpExplicitExpressionSyntax || children[1] is CSharpImplicitExpressionSyntax))
            {
                var containsNonWhitespace = literal.DescendantNodes()
                    .Where(n => n.IsToken)
                    .Cast<SyntaxToken>()
                    .Any(t => !string.IsNullOrWhiteSpace(t.Content));

                if (!containsNonWhitespace)
                {
                    // Literal node is all whitespace. Can rewrite.
                    whitespaceLiteral = SyntaxFactory.MarkupTextLiteral(literal.LiteralTokens);
                    rewritten = codeBlock.ReplaceNode(literal, newNode: null);
                    return true;
                }
            }

            return false;
        }
    }
}
