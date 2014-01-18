// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.Test.Framework;
using Microsoft.AspNet.Razor.Tokenizer.Symbols;
using Microsoft.TestCommon;

namespace Microsoft.AspNet.Razor.Test.Parser.VB
{
    public class VBNestedStatementsTest : VBHtmlCodeParserTestBase
    {
        [Fact]
        public void VB_Nested_If_Statement()
        {
            ParseBlockTest("@If True Then" + Environment.NewLine
                         + "    If False Then" + Environment.NewLine
                         + "    End If" + Environment.NewLine
                         + "End If",
                new StatementBlock(
                    Factory.CodeTransition(),
                    Factory.Code("If True Then\r\n    If False Then\r\n    End If\r\nEnd If")
                           .AsStatement()
                           .Accepts(AcceptedCharacters.None)));
        }

        [Fact]
        public void VB_Nested_Do_Statement()
        {
            ParseBlockTest("@Do While True" + Environment.NewLine
                         + "    Do" + Environment.NewLine
                         + "    Loop Until False" + Environment.NewLine
                         + "Loop",
                new StatementBlock(
                    Factory.CodeTransition(),
                    Factory.Code("Do While True\r\n    Do\r\n    Loop Until False\r\nLoop")
                           .AsStatement()
                           .Accepts(AcceptedCharacters.AnyExceptNewline)));
        }

        [Fact]
        public void VB_Nested_Markup_Statement_In_If()
        {
            ParseBlockTest("@If True Then" + Environment.NewLine
                         + "    @<p>Tag</p>" + Environment.NewLine
                         + "End If",
                new StatementBlock(
                    Factory.CodeTransition(),
                    Factory.Code("If True Then\r\n")
                           .AsStatement(),
                    new MarkupBlock(
                        Factory.Markup("    "),
                        Factory.MarkupTransition(),
                        Factory.Markup("<p>Tag</p>\r\n")
                               .Accepts(AcceptedCharacters.None)),
                    Factory.Code("End If")
                           .AsStatement()
                           .Accepts(AcceptedCharacters.None)));
        }

        [Fact]
        public void VB_Nested_Markup_Statement_In_Code()
        {
            ParseBlockTest("@Code" + Environment.NewLine
                         + "    Foo()" + Environment.NewLine
                         + "    @<p>Tag</p>" + Environment.NewLine
                         + "    Bar()" + Environment.NewLine
                         + "End Code",
                new StatementBlock(
                    Factory.CodeTransition(),
                    Factory.MetaCode("Code")
                           .Accepts(AcceptedCharacters.None),
                    Factory.Code("\r\n    Foo()\r\n")
                           .AsStatement(),
                    new MarkupBlock(
                        Factory.Markup("    "),
                        Factory.MarkupTransition(),
                        Factory.Markup("<p>Tag</p>\r\n")
                               .Accepts(AcceptedCharacters.None)),
                    Factory.Code("    Bar()\r\n")
                           .AsStatement(),
                    Factory.MetaCode("End Code")
                           .Accepts(AcceptedCharacters.None)));
        }

        [Fact]
        public void VB_Nested_Markup_Statement_In_Do()
        {
            ParseBlockTest("@Do" + Environment.NewLine
                         + "    @<p>Tag</p>" + Environment.NewLine
                         + "Loop While True",
                new StatementBlock(
                    Factory.CodeTransition(),
                    Factory.Code("Do\r\n")
                           .AsStatement(),
                    new MarkupBlock(
                        Factory.Markup("    "),
                        Factory.MarkupTransition(),
                        Factory.Markup("<p>Tag</p>\r\n")
                               .Accepts(AcceptedCharacters.None)),
                    Factory.Code("Loop While True")
                           .AsStatement()
                           .Accepts(AcceptedCharacters.AnyExceptNewline)));
        }

        [Fact]
        public void VB_Nested_Single_Line_Markup_Statement_In_Do()
        {
            ParseBlockTest("@Do" + Environment.NewLine
                         + "    @:<p>Tag" + Environment.NewLine
                         + "Loop While True",
                new StatementBlock(
                    Factory.CodeTransition(),
                    Factory.Code("Do\r\n")
                           .AsStatement(),
                    new MarkupBlock(
                        Factory.Markup("    "),
                        Factory.MarkupTransition(),
                        Factory.MetaMarkup(":", HtmlSymbolType.Colon),
                        Factory.Markup("<p>Tag\r\n")
                               .Accepts(AcceptedCharacters.None)),
                    Factory.Code("Loop While True")
                           .AsStatement()
                           .Accepts(AcceptedCharacters.AnyExceptNewline)));
        }

        [Fact]
        public void VB_Nested_Implicit_Expression_In_If()
        {
            ParseBlockTest("@If True Then" + Environment.NewLine
                         + "    @Foo.Bar" + Environment.NewLine
                         + "End If",
                new StatementBlock(
                    Factory.CodeTransition(),
                    Factory.Code("If True Then\r\n    ")
                           .AsStatement(),
                    new ExpressionBlock(
                        Factory.CodeTransition(),
                        Factory.Code("Foo.Bar")
                               .AsExpression()
                               .Accepts(AcceptedCharacters.NonWhiteSpace)),
                    Factory.Code("\r\nEnd If")
                           .AsStatement()
                           .Accepts(AcceptedCharacters.None)));
        }

        [Fact]
        public void VB_Nested_Explicit_Expression_In_If()
        {
            ParseBlockTest("@If True Then" + Environment.NewLine
                         + "    @(Foo.Bar + 42)" + Environment.NewLine
                         + "End If",
                new StatementBlock(
                    Factory.CodeTransition(),
                    Factory.Code("If True Then\r\n    ")
                           .AsStatement(),
                    new ExpressionBlock(
                        Factory.CodeTransition(),
                        Factory.MetaCode("(")
                               .Accepts(AcceptedCharacters.None),
                        Factory.Code("Foo.Bar + 42")
                               .AsExpression(),
                        Factory.MetaCode(")")
                               .Accepts(AcceptedCharacters.None)),
                    Factory.Code("\r\nEnd If")
                           .AsStatement()
                           .Accepts(AcceptedCharacters.None)));
        }
    }
}
