// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    public class WhiteSpaceRewriterTest
    {
        [Fact]
        public void Rewrite_Moves_Whitespace_Preceeding_ExpressionBlock_To_Parent_Block()
        {
            // Arrange
            var factory = new SpanFactory();
            var start = new MarkupBlock(
                factory.Markup("test"),
                new ExpressionBlock(
                    factory.Code("    ").AsExpression(),
                    factory.CodeTransition(SyntaxConstants.TransitionString),
                    factory.Code("foo").AsExpression()),
                factory.Markup("test"));
            var rewriter = new WhiteSpaceRewriter();

            // Act
            var rewritten = rewriter.Rewrite(start);
            factory.Reset();

            // Assert
            ParserTestBase.EvaluateParseTree(
                rewritten,
                new MarkupBlock(
                    factory.Markup("test"),
                    factory.Markup("    "),
                    new ExpressionBlock(
                        factory.CodeTransition(SyntaxConstants.TransitionString),
                        factory.Code("foo").AsExpression()),
                    factory.Markup("test")));
        }
    }
}
