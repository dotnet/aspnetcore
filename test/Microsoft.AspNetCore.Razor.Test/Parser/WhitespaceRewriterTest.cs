// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Parser.Internal;
using Microsoft.AspNetCore.Razor.Test.Framework;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Parser
{
    public class WhitespaceRewriterTest
    {
        [Fact]
        public void Rewrite_Moves_Whitespace_Preceeding_ExpressionBlock_To_Parent_Block()
        {
            // Arrange
            var factory = SpanFactory.CreateCsHtml();
            var start = new MarkupBlock(
                factory.Markup("test"),
                new ExpressionBlock(
                    factory.Code("    ").AsExpression(),
                    factory.CodeTransition(SyntaxConstants.TransitionString),
                    factory.Code("foo").AsExpression()
                    ),
                factory.Markup("test")
                );
            var rewriter = new WhiteSpaceRewriter(new HtmlMarkupParser().BuildSpan);
            var rewritingContext = new RewritingContext(start, new ErrorSink());

            // Act
            rewriter.Rewrite(rewritingContext);

            factory.Reset();

            // Assert
            ParserTestBase.EvaluateParseTree(rewritingContext.SyntaxTree, new MarkupBlock(
                                                         factory.Markup("test"),
                                                         factory.Markup("    "),
                                                         new ExpressionBlock(
                                                             factory.CodeTransition(SyntaxConstants.TransitionString),
                                                             factory.Code("foo").AsExpression()
                                                             ),
                                                         factory.Markup("test")
                                                         ));
        }
    }
}
