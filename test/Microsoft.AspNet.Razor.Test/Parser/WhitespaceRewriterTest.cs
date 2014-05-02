// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using Microsoft.AspNet.Razor.Parser;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.Test.Framework;
using Microsoft.TestCommon;

namespace Microsoft.AspNet.Razor.Test.Parser
{
    public class WhitespaceRewriterTest
    {
        [Fact]
        public void Constructor_Requires_NonNull_SymbolConverter()
        {
            Assert.ThrowsArgumentNull(() => new WhiteSpaceRewriter(null), "markupSpanFactory");
        }

        [Fact]
        public void Rewrite_Moves_Whitespace_Preceeding_ExpressionBlock_To_Parent_Block()
        {
            // Arrange
            var factory = SpanFactory.CreateCsHtml();
            Block start = new MarkupBlock(
                factory.Markup("test"),
                new ExpressionBlock(
                    factory.Code("    ").AsExpression(),
                    factory.CodeTransition(SyntaxConstants.TransitionString),
                    factory.Code("foo").AsExpression()
                    ),
                factory.Markup("test")
                );
            WhiteSpaceRewriter rewriter = new WhiteSpaceRewriter(new HtmlMarkupParser().BuildSpan);

            // Act
            Block actual = rewriter.Rewrite(start);

            factory.Reset();

            // Assert
            ParserTestBase.EvaluateParseTree(actual, new MarkupBlock(
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
