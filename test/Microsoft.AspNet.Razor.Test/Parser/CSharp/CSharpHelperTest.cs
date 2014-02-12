// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Razor.Generator;
using Microsoft.AspNet.Razor.Parser;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.Test.Framework;
using Microsoft.AspNet.Razor.Text;
using Microsoft.TestCommon;

namespace Microsoft.AspNet.Razor.Test.Parser.CSharp
{
    public class CSharpHelperTest : CsHtmlMarkupParserTestBase
    {
        [Fact]
        public void ParseHelperCorrectlyParsesHelperWithNoSpaceInBody()
        {
            ParseDocumentTest("@helper Foo(){@Bar()}",
                new MarkupBlock(
                    Factory.EmptyHtml(),
                    new HelperBlock(new HelperCodeGenerator(new LocationTagged<string>("Foo(){", 8, 0, 8), headerComplete: true),
                        Factory.CodeTransition(),
                        Factory.MetaCode("helper ").Accepts(AcceptedCharacters.None),
                        Factory.Code("Foo(){").Hidden().Accepts(AcceptedCharacters.None),
                        new StatementBlock(
                            Factory.EmptyCSharp().AsStatement(),
                            new ExpressionBlock(
                                Factory.CodeTransition(),
                                Factory.Code("Bar()")
                                    .AsImplicitExpression(CSharpCodeParser.DefaultKeywords, acceptTrailingDot: true)
                                    .Accepts(AcceptedCharacters.NonWhiteSpace)),
                            Factory.EmptyCSharp().AsStatement()),
                        Factory.Code("}").Hidden().Accepts(AcceptedCharacters.None)),
                    Factory.EmptyHtml()));
        }

        [Fact]
        public void ParseHelperCorrectlyParsesIncompleteHelperPreceedingCodeBlock()
        {
            ParseDocumentTest("@helper" + Environment.NewLine
                            + "@{}",
                new MarkupBlock(
                    Factory.EmptyHtml(),
                    new HelperBlock(
                        Factory.CodeTransition(),
                        Factory.MetaCode("helper")),
                    Factory.Markup("\r\n"),
                    new StatementBlock(
                        Factory.CodeTransition(),
                        Factory.MetaCode("{").Accepts(AcceptedCharacters.None),
                        Factory.EmptyCSharp().AsStatement(),
                        Factory.MetaCode("}").Accepts(AcceptedCharacters.None)),
                    Factory.EmptyHtml()),
                new RazorError(
                    RazorResources.ParseError_Unexpected_Character_At_Helper_Name_Start(
                        RazorResources.ErrorComponent_Newline),
                    7, 0, 7));
        }

        [Fact]
        public void ParseHelperRequiresSpaceBeforeSignature()
        {
            ParseDocumentTest("@helper{",
                new MarkupBlock(
                    Factory.EmptyHtml(),
                    new HelperBlock(
                        Factory.CodeTransition(),
                        Factory.MetaCode("helper")),
                    Factory.Markup("{")),
                new RazorError(
                    RazorResources.ParseError_Unexpected_Character_At_Helper_Name_Start(
                        RazorResources.ErrorComponent_Character("{")),
                    7, 0, 7));
        }

        [Fact]
        public void ParseHelperOutputsErrorButContinuesIfLParenFoundAfterHelperKeyword()
        {
            ParseDocumentTest("@helper () {",
                new MarkupBlock(
                    Factory.EmptyHtml(),
                    new HelperBlock(new HelperCodeGenerator(new LocationTagged<string>("() {", 8, 0, 8), headerComplete: true),
                        Factory.CodeTransition(),
                        Factory.MetaCode("helper ").Accepts(AcceptedCharacters.None),
                        Factory.Code("() {").Hidden().Accepts(AcceptedCharacters.None),
                        new StatementBlock(
                            Factory.EmptyCSharp()
                                   .AsStatement()
                                   .AutoCompleteWith("}")))),
                new RazorError(
                    RazorResources.ParseError_Unexpected_Character_At_Helper_Name_Start(
                        RazorResources.ErrorComponent_Character("(")),
                    8, 0, 8),
                new RazorError(
                    RazorResources.ParseError_Expected_EndOfBlock_Before_EOF(
                        "helper", "}", "{"),
                    1, 0, 1));
        }

        [Fact]
        public void ParseHelperStatementOutputsMarkerHelperHeaderSpanOnceKeywordComplete()
        {
            ParseDocumentTest("@helper ",
                new MarkupBlock(
                    Factory.EmptyHtml(),
                    new HelperBlock(new HelperCodeGenerator(new LocationTagged<string>(String.Empty, 8, 0, 8), headerComplete: false),
                        Factory.CodeTransition(),
                        Factory.MetaCode("helper ").Accepts(AcceptedCharacters.None),
                        Factory.EmptyCSharp().Hidden())),
                new RazorError(
                    RazorResources.ParseError_Unexpected_Character_At_Helper_Name_Start(
                        RazorResources.ErrorComponent_EndOfFile),
                    8, 0, 8));
        }

        [Fact]
        public void ParseHelperStatementMarksHelperSpanAsCanGrowIfMissingTrailingSpace()
        {
            ParseDocumentTest("@helper",
                new MarkupBlock(
                    Factory.EmptyHtml(),
                    new HelperBlock(
                        Factory.CodeTransition(),
                        Factory.MetaCode("helper").Accepts(AcceptedCharacters.Any))),
                new RazorError(
                    RazorResources.ParseError_Unexpected_Character_At_Helper_Name_Start(
                        RazorResources.ErrorComponent_EndOfFile),
                    7, 0, 7));
        }

        [Fact]
        public void ParseHelperStatementCapturesWhitespaceToEndOfLineIfHelperStatementMissingName()
        {
            ParseDocumentTest("@helper                       " + Environment.NewLine
                            + "    ",
                new MarkupBlock(
                    Factory.EmptyHtml(),
                    new HelperBlock(new HelperCodeGenerator(new LocationTagged<string>("                      ", 8, 0, 8), headerComplete: false),
                        Factory.CodeTransition(),
                        Factory.MetaCode("helper ").Accepts(AcceptedCharacters.None),
                        Factory.Code("                      \r\n").Hidden()),
                    Factory.Markup(@"    ")),
                new RazorError(
                    RazorResources.ParseError_Unexpected_Character_At_Helper_Name_Start(
                        RazorResources.ErrorComponent_Newline),
                    30, 0, 30));
        }

        [Fact]
        public void ParseHelperStatementCapturesWhitespaceToEndOfLineIfHelperStatementMissingOpenParen()
        {
            ParseDocumentTest("@helper Foo    " + Environment.NewLine
                            + "    ",
                new MarkupBlock(
                    Factory.EmptyHtml(),
                    new HelperBlock(new HelperCodeGenerator(new LocationTagged<string>("Foo    ", 8, 0, 8), headerComplete: false),
                        Factory.CodeTransition(),
                        Factory.MetaCode("helper ").Accepts(AcceptedCharacters.None),
                        Factory.Code("Foo    \r\n").Hidden()),
                    Factory.Markup("    ")),
                new RazorError(
                    RazorResources.ParseError_MissingCharAfterHelperName("("),
                    15, 0, 15));
        }

        [Fact]
        public void ParseHelperStatementCapturesAllContentToEndOfFileIfHelperStatementMissingCloseParenInParameterList()
        {
            ParseDocumentTest("@helper Foo(Foo Bar" + Environment.NewLine
                            + "Biz" + Environment.NewLine
                            + "Boz",
                new MarkupBlock(
                    Factory.EmptyHtml(),
                    new HelperBlock(new HelperCodeGenerator(new LocationTagged<string>("Foo(Foo Bar\r\nBiz\r\nBoz", 8, 0, 8), headerComplete: false),
                        Factory.CodeTransition(),
                        Factory.MetaCode("helper ").Accepts(AcceptedCharacters.None),
                        Factory.Code("Foo(Foo Bar\r\nBiz\r\nBoz").Hidden())),
                new RazorError(
                    RazorResources.ParseError_UnterminatedHelperParameterList,
                    11, 0, 11));
        }

        [Fact]
        public void ParseHelperStatementCapturesWhitespaceToEndOfLineIfHelperStatementMissingOpenBraceAfterParameterList()
        {
            ParseDocumentTest("@helper Foo(string foo)    " + Environment.NewLine,
                new MarkupBlock(
                    Factory.EmptyHtml(),
                    new HelperBlock(new HelperCodeGenerator(new LocationTagged<string>("Foo(string foo)    ", 8, 0, 8), headerComplete: false),
                        Factory.CodeTransition(),
                        Factory.MetaCode("helper ").Accepts(AcceptedCharacters.None),
                        Factory.Code("Foo(string foo)    \r\n").Hidden())),
                new RazorError(
                    RazorResources.ParseError_MissingCharAfterHelperParameters("{"),
                    29, 1, 0));
        }

        [Fact]
        public void ParseHelperStatementContinuesParsingHelperUntilEOF()
        {
            ParseDocumentTest("@helper Foo(string foo) {    " + Environment.NewLine
                            + "    <p>Foo</p>",
                new MarkupBlock(
                    Factory.EmptyHtml(),
                    new HelperBlock(new HelperCodeGenerator(new LocationTagged<string>("Foo(string foo) {", 8, 0, 8), headerComplete: true),
                        Factory.CodeTransition(),
                        Factory.MetaCode("helper ").Accepts(AcceptedCharacters.None),
                        Factory.Code(@"Foo(string foo) {").Hidden().Accepts(AcceptedCharacters.None),
                        new StatementBlock(
                            Factory.Code("    \r\n")
                                   .AsStatement()
                                   .AutoCompleteWith("}"),
                            new MarkupBlock(
                                Factory.Markup("    <p>Foo</p>").Accepts(AcceptedCharacters.None)),
                            Factory.EmptyCSharp().AsStatement()))),
                new RazorError(
                    RazorResources.ParseError_Expected_EndOfBlock_Before_EOF(
                        "helper", "}", "{"),
                    1, 0, 1));
        }

        [Fact]
        public void ParseHelperStatementCorrectlyParsesHelperWithEmbeddedCode()
        {
            ParseDocumentTest("@helper Foo(string foo) {    " + Environment.NewLine
                            + "    <p>@foo</p>" + Environment.NewLine
                            + "}",
                new MarkupBlock(
                    Factory.EmptyHtml(),
                    new HelperBlock(new HelperCodeGenerator(new LocationTagged<string>("Foo(string foo) {", 8, 0, 8), headerComplete: true),
                        Factory.CodeTransition(),
                        Factory.MetaCode("helper ").Accepts(AcceptedCharacters.None),
                        Factory.Code(@"Foo(string foo) {").Hidden().Accepts(AcceptedCharacters.None),
                        new StatementBlock(
                            Factory.Code("    \r\n").AsStatement(),
                            new MarkupBlock(
                                Factory.Markup("    <p>"),
                                new ExpressionBlock(
                                    Factory.CodeTransition(),
                                    Factory.Code("foo")
                                           .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                                           .Accepts(AcceptedCharacters.NonWhiteSpace)),
                                Factory.Markup("</p>\r\n").Accepts(AcceptedCharacters.None)),
                            Factory.EmptyCSharp().AsStatement()),
                        Factory.Code("}").Hidden().Accepts(AcceptedCharacters.None)),
                    Factory.EmptyHtml()));
        }

        [Fact]
        public void ParseHelperStatementCorrectlyParsesHelperWithNewlinesBetweenCloseParenAndOpenBrace()
        {
            ParseDocumentTest("@helper Foo(string foo)" + Environment.NewLine
                            + Environment.NewLine
                            + Environment.NewLine
                            + Environment.NewLine
                            + "{    " + Environment.NewLine
                            + "    <p>@foo</p>" + Environment.NewLine
                            + "}",
                new MarkupBlock(
                    Factory.EmptyHtml(),
                    new HelperBlock(new HelperCodeGenerator(new LocationTagged<string>("Foo(string foo)\r\n\r\n\r\n\r\n{", 8, 0, 8), headerComplete: true),
                        Factory.CodeTransition(),
                        Factory.MetaCode("helper ").Accepts(AcceptedCharacters.None),
                        Factory.Code("Foo(string foo)\r\n\r\n\r\n\r\n{").Hidden().Accepts(AcceptedCharacters.None),
                        new StatementBlock(
                            Factory.Code("    \r\n").AsStatement(),
                            new MarkupBlock(
                                Factory.Markup(@"    <p>"),
                                new ExpressionBlock(
                                    Factory.CodeTransition(),
                                    Factory.Code("foo")
                                           .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                                           .Accepts(AcceptedCharacters.NonWhiteSpace)),
                                Factory.Markup("</p>\r\n").Accepts(AcceptedCharacters.None)),
                            Factory.EmptyCSharp().AsStatement()),
                        Factory.Code("}").Hidden().Accepts(AcceptedCharacters.None)),
                    Factory.EmptyHtml()));
        }

        [Fact]
        public void ParseHelperStatementGivesWhitespaceAfterOpenBraceToMarkupInDesignMode()
        {
            ParseDocumentTest("@helper Foo(string foo) {    " + Environment.NewLine
                            + "    ",
                new MarkupBlock(
                    Factory.EmptyHtml(),
                    new HelperBlock(new HelperCodeGenerator(new LocationTagged<string>("Foo(string foo) {", 8, 0, 8), headerComplete: true),
                        Factory.CodeTransition(),
                        Factory.MetaCode("helper ").Accepts(AcceptedCharacters.None),
                        Factory.Code(@"Foo(string foo) {").Hidden().Accepts(AcceptedCharacters.None),
                        new StatementBlock(
                            Factory.Code("    \r\n    ")
                                   .AsStatement()
                                   .AutoCompleteWith("}")))),
                designTimeParser: true,
                expectedErrors: new[]
                {
                    new RazorError(
                        RazorResources.ParseError_Expected_EndOfBlock_Before_EOF( 
                            "helper", "}", "{"),
                        new SourceLocation(1, 0, 1))
                });
        }

        [Fact]
        public void ParseHelperAcceptsNestedHelpersButOutputsError()
        {
            ParseDocumentTest(@"@helper Foo(string foo) {" + Environment.NewLine
                            + "    @helper Bar(string baz) {" + Environment.NewLine
                            + "    }" + Environment.NewLine
                            + "}",
                new MarkupBlock(
                    Factory.EmptyHtml(),
                    new HelperBlock(new HelperCodeGenerator(new LocationTagged<string>("Foo(string foo) {", 8, 0, 8), headerComplete: true),
                        Factory.CodeTransition(),
                        Factory.MetaCode("helper ").Accepts(AcceptedCharacters.None),
                        Factory.Code(@"Foo(string foo) {").Hidden().Accepts(AcceptedCharacters.None),
                        new StatementBlock(
                            Factory.Code("\r\n    ").AsStatement(),
                            new HelperBlock(new HelperCodeGenerator(new LocationTagged<string>("Bar(string baz) {", 39, 1, 12), headerComplete: true),
                                Factory.CodeTransition(),
                                Factory.MetaCode("helper ").Accepts(AcceptedCharacters.None),
                                Factory.Code(@"Bar(string baz) {").Hidden().Accepts(AcceptedCharacters.None),
                                new StatementBlock(
                                    Factory.Code("\r\n    ").AsStatement()),
                                Factory.Code("}").Hidden().Accepts(AcceptedCharacters.None)),
                            Factory.Code("\r\n").AsStatement()),
                        Factory.Code("}").Hidden().Accepts(AcceptedCharacters.None)),
                    Factory.EmptyHtml()),
                designTimeParser: true,
                expectedErrors: new[]
                {
                    new RazorError(RazorResources.ParseError_Helpers_Cannot_Be_Nested, 38, 1, 11)
                });
        }
    }
}
