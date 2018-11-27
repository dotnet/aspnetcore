// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    public class CSharpNestedStatementsTest : CsHtmlCodeParserTestBase
    {
        [Fact]
        public void NestedSimpleStatement()
        {
            ParseBlockTest("@while(true) { foo(); }",
                new StatementBlock(
                    Factory.CodeTransition(),
                    Factory.Code("while(true) { foo(); }")
                           .AsStatement()
                           .Accepts(AcceptedCharactersInternal.None)));
        }

        [Fact]
        public void NestedKeywordStatement()
        {
            ParseBlockTest("@while(true) { for(int i = 0; i < 10; i++) { foo(); } }",
                new StatementBlock(
                    Factory.CodeTransition(),
                    Factory.Code("while(true) { for(int i = 0; i < 10; i++) { foo(); } }")
                           .AsStatement()
                           .Accepts(AcceptedCharactersInternal.None)));
        }

        [Fact]
        public void NestedCodeBlock()
        {
            ParseBlockTest("@while(true) { { { { foo(); } } } }",
                new StatementBlock(
                    Factory.CodeTransition(),
                    Factory.Code("while(true) { { { { foo(); } } } }")
                           .AsStatement()
                           .Accepts(AcceptedCharactersInternal.None)));
        }

        [Fact]
        public void NestedImplicitExpression()
        {
            ParseBlockTest("@while(true) { @foo }",
                new StatementBlock(
                    Factory.CodeTransition(),
                    Factory.Code("while(true) { ")
                           .AsStatement(),
                    new ExpressionBlock(
                        Factory.CodeTransition(),
                        Factory.Code("foo")
                               .AsImplicitExpression(CSharpCodeParser.DefaultKeywords, acceptTrailingDot: true)
                               .Accepts(AcceptedCharactersInternal.NonWhiteSpace)),
                    Factory.Code(" }")
                           .AsStatement()
                           .Accepts(AcceptedCharactersInternal.None)));
        }

        [Fact]
        public void NestedExplicitExpression()
        {
            ParseBlockTest("@while(true) { @(foo) }",
                new StatementBlock(
                    Factory.CodeTransition(),
                    Factory.Code("while(true) { ")
                           .AsStatement(),
                    new ExpressionBlock(
                        Factory.CodeTransition(),
                        Factory.MetaCode("(")
                               .Accepts(AcceptedCharactersInternal.None),
                        Factory.Code("foo")
                               .AsExpression(),
                        Factory.MetaCode(")")
                               .Accepts(AcceptedCharactersInternal.None)),
                    Factory.Code(" }")
                           .AsStatement()
                           .Accepts(AcceptedCharactersInternal.None)));
        }

        [Fact]
        public void NestedMarkupBlock()
        {
            ParseBlockTest("@while(true) { <p>Hello</p> }",
                new StatementBlock(
                    Factory.CodeTransition(),
                    Factory.Code("while(true) {")
                           .AsStatement(),
                    new MarkupBlock(
                        Factory.Markup(" "),
                        new MarkupTagBlock(
                            Factory.Markup("<p>").Accepts(AcceptedCharactersInternal.None)),
                        Factory.Markup("Hello"),
                        new MarkupTagBlock(
                            Factory.Markup("</p>").Accepts(AcceptedCharactersInternal.None)),
                        Factory.Markup(" ").Accepts(AcceptedCharactersInternal.None)
                        ),
                    Factory.Code("}")
                           .AsStatement()
                           .Accepts(AcceptedCharactersInternal.None)));
        }
    }
}
