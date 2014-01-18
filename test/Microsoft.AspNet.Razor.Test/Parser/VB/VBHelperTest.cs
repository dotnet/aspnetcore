// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Razor.Generator;
using Microsoft.AspNet.Razor.Parser;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.Resources;
using Microsoft.AspNet.Razor.Test.Framework;
using Microsoft.AspNet.Razor.Text;
using Microsoft.TestCommon;

namespace Microsoft.AspNet.Razor.Test.Parser.VB
{
    public class VBHelperTest : VBHtmlMarkupParserTestBase
    {
        [Fact]
        public void ParseHelperOutputsErrorButContinuesIfLParenFoundAfterHelperKeyword()
        {
            ParseDocumentTest("@Helper ()",
                new MarkupBlock(
                    Factory.EmptyHtml(),
                    new HelperBlock(new HelperCodeGenerator(new LocationTagged<string>("()", 8, 0, 8), headerComplete: true),
                        Factory.CodeTransition(),
                        Factory.MetaCode("Helper ").Accepts(AcceptedCharacters.None),
                        Factory.Code("()").Hidden().AutoCompleteWith(SyntaxConstants.VB.EndHelperKeyword),
                        new StatementBlock())),
                new RazorError(
                    String.Format(
                        RazorResources.ParseError_Unexpected_Character_At_Helper_Name_Start,
                        String.Format(RazorResources.ErrorComponent_Character, "(")),
                    8, 0, 8),
                new RazorError(
                    String.Format(RazorResources.ParseError_BlockNotTerminated, "Helper", "End Helper"),
                    1, 0, 1));
        }

        [Fact]
        public void ParseHelperStatementOutputsMarkerHelperHeaderSpanOnceKeywordComplete()
        {
            ParseDocumentTest("@Helper ",
                new MarkupBlock(
                    Factory.EmptyHtml(),
                    new HelperBlock(new HelperCodeGenerator(new LocationTagged<string>(String.Empty, 8, 0, 8), headerComplete: false),
                        Factory.CodeTransition(),
                        Factory.MetaCode("Helper ").Accepts(AcceptedCharacters.None),
                        Factory.EmptyVB().Hidden())),
                new RazorError(
                    String.Format(RazorResources.ParseError_Unexpected_Character_At_Helper_Name_Start, RazorResources.ErrorComponent_EndOfFile),
                    8, 0, 8));
        }

        [Fact]
        public void ParseHelperStatementMarksHelperSpanAsCanGrowIfMissingTrailingSpace()
        {
            ParseDocumentTest("@Helper",
                new MarkupBlock(
                    Factory.EmptyHtml(),
                    new HelperBlock(
                        Factory.CodeTransition(),
                        Factory.MetaCode("Helper"))),
                new RazorError(
                    String.Format(RazorResources.ParseError_Unexpected_Character_At_Helper_Name_Start, RazorResources.ErrorComponent_EndOfFile),
                    7, 0, 7));
        }

        [Fact]
        public void ParseHelperStatementTerminatesEarlyIfHeaderNotComplete()
        {
            ParseDocumentTest("@Helper" + Environment.NewLine
                            + "@Helper",
                new MarkupBlock(
                    Factory.EmptyHtml(),
                    new HelperBlock(
                        Factory.CodeTransition(),
                        Factory.MetaCode("Helper\r\n").Accepts(AcceptedCharacters.None),
                        Factory.EmptyVB().Hidden()),
                    new HelperBlock(
                        Factory.CodeTransition(),
                        Factory.MetaCode("Helper"))),
                designTimeParser: true,
                expectedErrors: new[]
                {
                    new RazorError(
                        String.Format(
                            RazorResources.ParseError_Unexpected_Character_At_Helper_Name_Start, 
                            String.Format(RazorResources.ErrorComponent_Character, "@")),
                        9, 1, 0),
                    new RazorError(
                        String.Format(
                            RazorResources.ParseError_Unexpected_Character_At_Helper_Name_Start, 
                            RazorResources.ErrorComponent_EndOfFile),
                        16, 1, 7)
                });
        }

        [Fact]
        public void ParseHelperStatementTerminatesEarlyIfHeaderNotCompleteWithSpace()
        {
            ParseDocumentTest(@"@Helper @Helper",
                new MarkupBlock(
                    Factory.EmptyHtml(),
                    new HelperBlock(new HelperCodeGenerator(new LocationTagged<string>(String.Empty, 8, 0, 8), headerComplete: false),
                        Factory.CodeTransition(),
                        Factory.MetaCode(@"Helper ").Accepts(AcceptedCharacters.None),
                        Factory.EmptyVB().Hidden()),
                    new HelperBlock(
                        Factory.CodeTransition(),
                        Factory.MetaCode("Helper").Accepts(AcceptedCharacters.Any))),
                designTimeParser: true,
                expectedErrors: new[]
                {
                    new RazorError(
                        String.Format(
                            RazorResources.ParseError_Unexpected_Character_At_Helper_Name_Start,
                            String.Format(RazorResources.ErrorComponent_Character, "@")),
                        8, 0, 8),
                    new RazorError(
                        String.Format(
                            RazorResources.ParseError_Unexpected_Character_At_Helper_Name_Start, 
                            RazorResources.ErrorComponent_EndOfFile),
                        15, 0, 15)
                });
        }

        [Fact]
        public void ParseHelperStatementAllowsDifferentlyCasedEndHelperKeyword()
        {
            ParseDocumentTest("@Helper Foo()" + Environment.NewLine
                            + "end helper",
                new MarkupBlock(
                    Factory.EmptyHtml(),
                    new HelperBlock(new HelperCodeGenerator(new LocationTagged<string>("Foo()", 8, 0, 8), headerComplete: true),
                        Factory.CodeTransition(),
                        Factory.MetaCode("Helper ").Accepts(AcceptedCharacters.None),
                        Factory.Code("Foo()").Hidden(),
                        new StatementBlock(
                            Factory.Code("\r\n").AsStatement(),
                            Factory.MetaCode("end helper").Accepts(AcceptedCharacters.None))),
                    Factory.EmptyHtml()));
        }

        [Fact]
        public void ParseHelperStatementCapturesWhitespaceToEndOfLineIfHelperStatementMissingName()
        {
            ParseDocumentTest("@Helper                       " + Environment.NewLine
                            + "    ",
                new MarkupBlock(
                    Factory.EmptyHtml(),
                    new HelperBlock(new HelperCodeGenerator(new LocationTagged<string>("                      ", 8, 0, 8), headerComplete: false),
                        Factory.CodeTransition(),
                        Factory.MetaCode("Helper ").Accepts(AcceptedCharacters.None),
                        Factory.Code("                      ").Hidden()),
                    Factory.Markup("\r\n    ")),
                new RazorError(
                    String.Format(
                        RazorResources.ParseError_Unexpected_Character_At_Helper_Name_Start,
                        RazorResources.ErrorComponent_Newline),
                    30, 0, 30));
        }

        [Fact]
        public void ParseHelperStatementCapturesWhitespaceToEndOfLineIfHelperStatementMissingOpenParen()
        {
            ParseDocumentTest("@Helper Foo    " + Environment.NewLine
                            + "    ",
                new MarkupBlock(
                    Factory.EmptyHtml(),
                    new HelperBlock(new HelperCodeGenerator(new LocationTagged<string>("Foo    ", 8, 0, 8), headerComplete: false),
                        Factory.CodeTransition(),
                        Factory.MetaCode("Helper ").Accepts(AcceptedCharacters.None),
                        Factory.Code("Foo    ").Hidden()),
                    Factory.Markup("\r\n    ")),
                new RazorError(
                    String.Format(RazorResources.ParseError_MissingCharAfterHelperName, "("),
                    15, 0, 15));
        }

        [Fact]
        public void ParseHelperStatementCapturesAllContentToEndOfFileIfHelperStatementMissingCloseParenInParameterList()
        {
            ParseDocumentTest("@Helper Foo(Foo Bar" + Environment.NewLine
                            + "Biz" + Environment.NewLine
                            + "Boz",
                new MarkupBlock(
                    Factory.EmptyHtml(),
                    new HelperBlock(new HelperCodeGenerator(new LocationTagged<string>("Foo(Foo Bar\r\nBiz\r\nBoz", 8, 0, 8), headerComplete: false),
                        Factory.CodeTransition(),
                        Factory.MetaCode("Helper ").Accepts(AcceptedCharacters.None),
                        Factory.Code("Foo(Foo Bar\r\nBiz\r\nBoz").Hidden())),
                new RazorError(RazorResources.ParseError_UnterminatedHelperParameterList, 11, 0, 11));
        }

        [Fact]
        public void ParseHelperStatementCapturesWhitespaceToEndOfLineIfHelperStatementMissingOpenBraceAfterParameterList()
        {
            ParseDocumentTest("@Helper Foo(foo as String)    " + Environment.NewLine,
                new MarkupBlock(
                    Factory.EmptyHtml(),
                    new HelperBlock(new HelperCodeGenerator(new LocationTagged<string>("Foo(foo as String)", 8, 0, 8), headerComplete: true),
                        Factory.CodeTransition(),
                        Factory.MetaCode("Helper ").Accepts(AcceptedCharacters.None),
                        Factory.Code("Foo(foo as String)")
                               .Hidden()
                               .AutoCompleteWith(SyntaxConstants.VB.EndHelperKeyword),
                        new StatementBlock(
                            Factory.Code("    \r\n").AsStatement()))),
                new RazorError(
                    String.Format(RazorResources.ParseError_BlockNotTerminated, "Helper", "End Helper"),
                    1, 0, 1));
        }

        [Fact]
        public void ParseHelperStatementContinuesParsingHelperUntilEOF()
        {
            ParseDocumentTest("@Helper Foo(foo as String)" + Environment.NewLine
                            + "    @<p>Foo</p>",
                new MarkupBlock(
                    Factory.EmptyHtml(),
                    new HelperBlock(new HelperCodeGenerator(new LocationTagged<string>("Foo(foo as String)", 8, 0, 8), headerComplete: true),
                        Factory.CodeTransition(),
                        Factory.MetaCode("Helper ").Accepts(AcceptedCharacters.None),
                        Factory.Code("Foo(foo as String)")
                               .Hidden()
                               .AutoCompleteWith(SyntaxConstants.VB.EndHelperKeyword),
                        new StatementBlock(
                            Factory.Code("\r\n").AsStatement(),
                            new MarkupBlock(
                                Factory.Markup("    "),
                                Factory.MarkupTransition(),
                                Factory.Markup("<p>Foo</p>").Accepts(AcceptedCharacters.None)),
                            Factory.EmptyVB().AsStatement()))),
                new RazorError(
                    String.Format(RazorResources.ParseError_BlockNotTerminated, "Helper", "End Helper"),
                    1, 0, 1));
        }

        [Fact]
        public void ParseHelperStatementCorrectlyParsesHelperWithEmbeddedCode()
        {
            ParseDocumentTest("@Helper Foo(foo as String, bar as String)" + Environment.NewLine
                            + "    @<p>@foo</p>" + Environment.NewLine
                            + "End Helper",
                new MarkupBlock(
                    Factory.EmptyHtml(),
                    new HelperBlock(new HelperCodeGenerator(new LocationTagged<string>("Foo(foo as String, bar as String)", 8, 0, 8), headerComplete: true),
                        Factory.CodeTransition(),
                        Factory.MetaCode("Helper ").Accepts(AcceptedCharacters.None),
                        Factory.Code("Foo(foo as String, bar as String)").Hidden(),
                        new StatementBlock(
                            Factory.Code("\r\n").AsStatement(),
                            new MarkupBlock(
                                Factory.Markup("    "),
                                Factory.MarkupTransition(),
                                Factory.Markup("<p>"),
                                new ExpressionBlock(
                                    Factory.CodeTransition(),
                                    Factory.Code("foo")
                                           .AsImplicitExpression(VBCodeParser.DefaultKeywords)
                                           .Accepts(AcceptedCharacters.NonWhiteSpace)),
                                Factory.Markup("</p>\r\n").Accepts(AcceptedCharacters.None)),
                            Factory.EmptyVB().AsStatement(),
                            Factory.MetaCode("End Helper").Accepts(AcceptedCharacters.None))),
                    Factory.EmptyHtml()));
        }

        [Fact]
        public void ParseHelperStatementGivesWhitespaceAfterCloseParenToMarkup()
        {
            ParseDocumentTest("@Helper Foo(string foo)     " + Environment.NewLine
                            + "    ",
                new MarkupBlock(
                    Factory.EmptyHtml(),
                    new HelperBlock(new HelperCodeGenerator(new LocationTagged<string>("Foo(string foo)", 8, 0, 8), headerComplete: true),
                        Factory.CodeTransition(),
                        Factory.MetaCode("Helper ").Accepts(AcceptedCharacters.None),
                        Factory.Code("Foo(string foo)")
                               .Hidden()
                               .AutoCompleteWith(SyntaxConstants.VB.EndHelperKeyword),
                        new StatementBlock(
                            Factory.Code("     \r\n    ").AsStatement()))),
                designTimeParser: true,
                expectedErrors:
                    new RazorError(
                        String.Format(
                            RazorResources.ParseError_BlockNotTerminated,
                            "Helper", "End Helper"),
                        1, 0, 1));
        }

        [Fact]
        public void ParseHelperAcceptsNestedHelpersButOutputsError()
        {
            ParseDocumentTest("@Helper Foo(string foo)" + Environment.NewLine
                            + "    @Helper Bar(string baz)" + Environment.NewLine
                            + "    End Helper" + Environment.NewLine
                            + "End Helper",
                new MarkupBlock(
                    Factory.EmptyHtml(),
                    new HelperBlock(new HelperCodeGenerator(new LocationTagged<string>("Foo(string foo)", 8, 0, 8), headerComplete: true),
                        Factory.CodeTransition(),
                        Factory.MetaCode("Helper ").Accepts(AcceptedCharacters.None),
                        Factory.Code("Foo(string foo)").Hidden(),
                        new StatementBlock(
                            Factory.Code("\r\n    ").AsStatement(),
                            new HelperBlock(new HelperCodeGenerator(new LocationTagged<string>("Bar(string baz)", 37, 1, 12), headerComplete: true),
                                Factory.CodeTransition(),
                                Factory.MetaCode("Helper ").Accepts(AcceptedCharacters.None),
                                Factory.Code("Bar(string baz)").Hidden(),
                                new StatementBlock(
                                    Factory.Code("\r\n    ").AsStatement(),
                                    Factory.MetaCode("End Helper").Accepts(AcceptedCharacters.None))),
                            Factory.Code("\r\n").AsStatement(),
                            Factory.MetaCode("End Helper").Accepts(AcceptedCharacters.None))),
                    Factory.EmptyHtml()),
                designTimeParser: true,
                expectedErrors: new[]
                {
                    new RazorError(
                        RazorResources.ParseError_Helpers_Cannot_Be_Nested,
                        30, 1, 5)
                });
        }
    }
}
