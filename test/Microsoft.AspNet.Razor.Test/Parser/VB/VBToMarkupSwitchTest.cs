// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Razor.Editor;
using Microsoft.AspNet.Razor.Parser;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.Test.Framework;
using Microsoft.AspNet.Razor.Tokenizer.Symbols;
using Microsoft.TestCommon;

namespace Microsoft.AspNet.Razor.Test.Parser.VB
{
    public class VBToMarkupSwitchTest : VBHtmlCodeParserTestBase
    {
        [Fact]
        public void ParseBlockSwitchesToMarkupWhenAtSignFollowedByLessThanInStatementBlock()
        {
            ParseBlockTest("Code" + Environment.NewLine
                         + "    If True Then" + Environment.NewLine
                         + "        @<p>It's True!</p>" + Environment.NewLine
                         + "    End If" + Environment.NewLine
                         + "End Code",
                new StatementBlock(
                    Factory.MetaCode("Code").Accepts(AcceptedCharacters.None),
                    Factory.Code("\r\n    If True Then\r\n").AsStatement(),
                    new MarkupBlock(
                        Factory.Markup("        "),
                        Factory.MarkupTransition(),
                        Factory.Markup("<p>It's True!</p>\r\n").Accepts(AcceptedCharacters.None)),
                    Factory.Code("    End If\r\n").AsStatement(),
                    Factory.MetaCode("End Code").Accepts(AcceptedCharacters.None)));
        }

        [Fact]
        public void ParseBlockGivesWhiteSpacePreceedingMarkupBlockToCodeInDesignTimeMode()
        {
            ParseBlockTest("Code" + Environment.NewLine
                         + "    @<p>Foo</p>" + Environment.NewLine
                         + "End Code",
                new StatementBlock(
                    Factory.MetaCode("Code").Accepts(AcceptedCharacters.None),
                    Factory.Code("\r\n    ").AsStatement(),
                    new MarkupBlock(
                        Factory.MarkupTransition(),
                        Factory.Markup("<p>Foo</p>").Accepts(AcceptedCharacters.None)),
                    Factory.Code("\r\n").AsStatement(),
                    Factory.MetaCode("End Code").Accepts(AcceptedCharacters.None)),
                designTimeParser: true);
        }

        [Theory]
        [InlineData("While", "End While", AcceptedCharacters.None)]
        [InlineData("If", "End If", AcceptedCharacters.None)]
        [InlineData("Select", "End Select", AcceptedCharacters.None)]
        [InlineData("For", "Next", AcceptedCharacters.WhiteSpace | AcceptedCharacters.NonWhiteSpace)]
        [InlineData("Try", "End Try", AcceptedCharacters.None)]
        [InlineData("With", "End With", AcceptedCharacters.None)]
        [InlineData("Using", "End Using", AcceptedCharacters.None)]
        public void SimpleMarkupSwitch(string keyword, string endSequence, AcceptedCharacters acceptedCharacters)
        {
            ParseBlockTest(keyword + Environment.NewLine
                         + "    If True Then" + Environment.NewLine
                         + "        @<p>It's True!</p>" + Environment.NewLine
                         + "    End If" + Environment.NewLine
                         + endSequence,
                new StatementBlock(
                    Factory.Code(keyword + "\r\n    If True Then\r\n").AsStatement(),
                    new MarkupBlock(
                        Factory.Markup("        "),
                        Factory.MarkupTransition(),
                        Factory.Markup("<p>It's True!</p>\r\n").Accepts(AcceptedCharacters.None)),
                    Factory.Code("    End If\r\n" + endSequence).AsStatement().Accepts(acceptedCharacters)));
        }

        [Theory]
        [InlineData("While", "End While", AcceptedCharacters.None)]
        [InlineData("If", "End If", AcceptedCharacters.None)]
        [InlineData("Select", "End Select", AcceptedCharacters.None)]
        [InlineData("For", "Next", AcceptedCharacters.WhiteSpace | AcceptedCharacters.NonWhiteSpace)]
        [InlineData("Try", "End Try", AcceptedCharacters.None)]
        [InlineData("With", "End With", AcceptedCharacters.None)]
        [InlineData("Using", "End Using", AcceptedCharacters.None)]
        public void SingleLineMarkupSwitch(string keyword, string endSequence, AcceptedCharacters acceptedCharacters)
        {
            ParseBlockTest(keyword + Environment.NewLine
                         + "    If True Then" + Environment.NewLine
                         + "        @:<p>It's True!</p>" + Environment.NewLine
                         + "        This is code!" + Environment.NewLine
                         + "    End If" + Environment.NewLine
                         + endSequence,
                new StatementBlock(
                    Factory.Code(keyword + "\r\n    If True Then\r\n").AsStatement(),
                    new MarkupBlock(
                        Factory.Markup("        "),
                        Factory.MarkupTransition(),
                        Factory.MetaMarkup(":", HtmlSymbolType.Colon),
                        Factory.Markup("<p>It's True!</p>\r\n")
                                .With(new SingleLineMarkupEditHandler(CSharpLanguageCharacteristics.Instance.TokenizeString))
                                .Accepts(AcceptedCharacters.None)),
                    Factory.Code("        This is code!\r\n    End If\r\n" + endSequence)
                            .AsStatement()
                            .Accepts(acceptedCharacters)));
        }
    }
}
