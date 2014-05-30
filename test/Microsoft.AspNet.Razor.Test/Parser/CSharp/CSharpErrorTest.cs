// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Razor.Parser;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.Test.Framework;
using Microsoft.AspNet.Razor.Text;
using Xunit;

namespace Microsoft.AspNet.Razor.Test.Parser.CSharp
{
    public class CSharpErrorTest : CsHtmlCodeParserTestBase
    {
        [Fact]
        public void ParseBlockHandlesQuotesAfterTransition()
        {
            ParseBlockTest("@\"",
                           new ExpressionBlock(
                               Factory.CodeTransition(),
                               Factory.EmptyCSharp()
                                   .AsImplicitExpression(KeywordSet)
                                   .Accepts(AcceptedCharacters.NonWhiteSpace)
                               ),
                           new RazorError(
                               RazorResources.FormatParseError_Unexpected_Character_At_Start_Of_CodeBlock_CS('"'),
                               1, 0, 1));
        }

        [Fact]
        public void ParseBlockCapturesWhitespaceToEndOfLineInInvalidUsingStatementAndTreatsAsFileCode()
        {
            ParseBlockTest("using          " + Environment.NewLine
                         + Environment.NewLine,
                           new StatementBlock(
                               Factory.Code("using          \r\n").AsStatement()
                               ));
        }

        [Fact]
        public void ParseBlockMethodOutputsOpenCurlyAsCodeSpanIfEofFoundAfterOpenCurlyBrace()
        {
            ParseBlockTest("{",
                           new StatementBlock(
                               Factory.MetaCode("{").Accepts(AcceptedCharacters.None),
                               Factory.EmptyCSharp()
                                   .AsStatement()
                                   .With(new AutoCompleteEditHandler(CSharpLanguageCharacteristics.Instance.TokenizeString) { AutoCompleteString = "}" })
                               ),
                           new RazorError(
                               RazorResources.FormatParseError_Expected_EndOfBlock_Before_EOF(RazorResources.BlockName_Code, "}", "{"),
                               SourceLocation.Zero));
        }

        [Fact]
        public void ParseBlockMethodOutputsZeroLengthCodeSpanIfStatementBlockEmpty()
        {
            ParseBlockTest("{}",
                           new StatementBlock(
                               Factory.MetaCode("{").Accepts(AcceptedCharacters.None),
                               Factory.EmptyCSharp().AsStatement(),
                               Factory.MetaCode("}").Accepts(AcceptedCharacters.None)
                               ));
        }

        [Fact]
        public void ParseBlockMethodProducesErrorIfNewlineFollowsTransition()
        {
            ParseBlockTest("@" + Environment.NewLine,
                           new ExpressionBlock(
                               Factory.CodeTransition(),
                               Factory.EmptyCSharp()
                                   .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                                   .Accepts(AcceptedCharacters.NonWhiteSpace)),
                           new RazorError(RazorResources.ParseError_Unexpected_WhiteSpace_At_Start_Of_CodeBlock_CS, new SourceLocation(1, 0, 1)));
        }

        [Fact]
        public void ParseBlockMethodProducesErrorIfWhitespaceBetweenTransitionAndBlockStartInEmbeddedExpression()
        {
            ParseBlockTest("{" + Environment.NewLine
                         + "    @   {}" + Environment.NewLine
                         + "}",
                           new StatementBlock(
                               Factory.MetaCode("{").Accepts(AcceptedCharacters.None),
                               Factory.Code("\r\n    ").AsStatement(),
                               new ExpressionBlock(
                                   Factory.CodeTransition(),
                                   Factory.EmptyCSharp()
                                       .AsImplicitExpression(CSharpCodeParser.DefaultKeywords, acceptTrailingDot: true)
                                       .Accepts(AcceptedCharacters.NonWhiteSpace)),
                               Factory.Code("   {}\r\n").AsStatement(),
                               Factory.MetaCode("}").Accepts(AcceptedCharacters.None)
                               ),
                           new RazorError(RazorResources.ParseError_Unexpected_WhiteSpace_At_Start_Of_CodeBlock_CS, 8, 1, 5));
        }

        [Fact]
        public void ParseBlockMethodProducesErrorIfEOFAfterTransitionInEmbeddedExpression()
        {
            ParseBlockTest("{" + Environment.NewLine
                         + "    @",
                           new StatementBlock(
                               Factory.MetaCode("{").Accepts(AcceptedCharacters.None),
                               Factory.Code("\r\n    ").AsStatement(),
                               new ExpressionBlock(
                                   Factory.CodeTransition(),
                                   Factory.EmptyCSharp()
                                       .AsImplicitExpression(CSharpCodeParser.DefaultKeywords, acceptTrailingDot: true)
                                       .Accepts(AcceptedCharacters.NonWhiteSpace)),
                               Factory.EmptyCSharp().AsStatement()
                               ),
                           new RazorError(RazorResources.ParseError_Unexpected_EndOfFile_At_Start_Of_CodeBlock, 8, 1, 5),
                           new RazorError(
                               RazorResources.FormatParseError_Expected_EndOfBlock_Before_EOF(RazorResources.BlockName_Code, "}", "{"),
                               SourceLocation.Zero));
        }

        [Fact]
        public void ParseBlockMethodParsesNothingIfFirstCharacterIsNotIdentifierStartOrParenOrBrace()
        {
            ParseBlockTest("@!!!",
                           new ExpressionBlock(
                               Factory.CodeTransition(),
                               Factory.EmptyCSharp()
                                   .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                                   .Accepts(AcceptedCharacters.NonWhiteSpace)),
                           new RazorError(
                               RazorResources.FormatParseError_Unexpected_Character_At_Start_Of_CodeBlock_CS("!"),
                               1, 0, 1));
        }

        [Fact]
        public void ParseBlockShouldReportErrorAndTerminateAtEOFIfIfParenInExplicitExpressionUnclosed()
        {
            ParseBlockTest("(foo bar" + Environment.NewLine
                         + "baz",
                           new ExpressionBlock(
                               Factory.MetaCode("(").Accepts(AcceptedCharacters.None),
                               Factory.Code("foo bar\r\nbaz").AsExpression()
                               ),
                           new RazorError(
                               RazorResources.FormatParseError_Expected_EndOfBlock_Before_EOF(RazorResources.BlockName_ExplicitExpression, ')', '('),
                               new SourceLocation(0, 0, 0)));
        }

        [Fact]
        public void ParseBlockShouldReportErrorAndTerminateAtMarkupIfIfParenInExplicitExpressionUnclosed()
        {
            ParseBlockTest("(foo bar" + Environment.NewLine
                         + "<html>" + Environment.NewLine
                         + "baz" + Environment.NewLine
                         + "</html",
                           new ExpressionBlock(
                               Factory.MetaCode("(").Accepts(AcceptedCharacters.None),
                               Factory.Code("foo bar\r\n").AsExpression()
                               ),
                           new RazorError(
                               RazorResources.FormatParseError_Expected_EndOfBlock_Before_EOF(RazorResources.BlockName_ExplicitExpression, ')', '('),
                               new SourceLocation(0, 0, 0)));
        }

        [Fact]
        public void ParseBlockCorrectlyHandlesInCorrectTransitionsIfImplicitExpressionParensUnclosed()
        {
            ParseBlockTest("Href(" + Environment.NewLine
                         + "<h1>@Html.Foo(Bar);</h1>" + Environment.NewLine,
                           new ExpressionBlock(
                               Factory.Code("Href(\r\n")
                                   .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                               ),
                           new RazorError(
                               RazorResources.FormatParseError_Expected_CloseBracket_Before_EOF("(", ")"),
                               new SourceLocation(4, 0, 4)));
        }

        [Fact]
        // Test for fix to Dev10 884975 - Incorrect Error Messaging
        public void ParseBlockShouldReportErrorAndTerminateAtEOFIfParenInImplicitExpressionUnclosed()
        {
            ParseBlockTest("Foo(Bar(Baz)" + Environment.NewLine
                         + "Biz" + Environment.NewLine
                         + "Boz",
                           new ExpressionBlock(
                               Factory.Code("Foo(Bar(Baz)\r\nBiz\r\nBoz")
                                   .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                               ),
                           new RazorError(RazorResources.FormatParseError_Expected_CloseBracket_Before_EOF("(", ")"),
                                          new SourceLocation(3, 0, 3)));
        }

        [Fact]
        // Test for fix to Dev10 884975 - Incorrect Error Messaging
        public void ParseBlockShouldReportErrorAndTerminateAtMarkupIfParenInImplicitExpressionUnclosed()
        {
            ParseBlockTest("Foo(Bar(Baz)" + Environment.NewLine
                         + "Biz" + Environment.NewLine
                         + "<html>" + Environment.NewLine
                         + "Boz" + Environment.NewLine
                         + "</html>",
                           new ExpressionBlock(
                               Factory.Code("Foo(Bar(Baz)\r\nBiz\r\n")
                                   .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                               ),
                           new RazorError(RazorResources.FormatParseError_Expected_CloseBracket_Before_EOF("(", ")"),
                                          new SourceLocation(3, 0, 3)));
        }

        [Fact]
        // Test for fix to Dev10 884975 - Incorrect Error Messaging
        public void ParseBlockShouldReportErrorAndTerminateAtEOFIfBracketInImplicitExpressionUnclosed()
        {
            ParseBlockTest("Foo[Bar[Baz]" + Environment.NewLine
                         + "Biz" + Environment.NewLine
                         + "Boz",
                           new ExpressionBlock(
                               Factory.Code("Foo[Bar[Baz]\r\nBiz\r\nBoz")
                                   .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                               ),
                           new RazorError(
                               RazorResources.FormatParseError_Expected_CloseBracket_Before_EOF("[", "]"),
                               new SourceLocation(3, 0, 3)));
        }

        [Fact]
        // Test for fix to Dev10 884975 - Incorrect Error Messaging
        public void ParseBlockShouldReportErrorAndTerminateAtMarkupIfBracketInImplicitExpressionUnclosed()
        {
            ParseBlockTest("Foo[Bar[Baz]" + Environment.NewLine
                         + "Biz" + Environment.NewLine
                         + "<b>" + Environment.NewLine
                         + "Boz" + Environment.NewLine
                         + "</b>",
                           new ExpressionBlock(
                               Factory.Code("Foo[Bar[Baz]\r\nBiz\r\n")
                                   .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                               ),
                           new RazorError(
                               RazorResources.FormatParseError_Expected_CloseBracket_Before_EOF("[", "]"),
                               new SourceLocation(3, 0, 3)));
        }

        // Simple EOF handling errors:
        [Fact]
        public void ParseBlockReportsErrorIfExplicitCodeBlockUnterminatedAtEOF()
        {
            ParseBlockTest("{ var foo = bar; if(foo != null) { bar(); } ",
                           new StatementBlock(
                               Factory.MetaCode("{").Accepts(AcceptedCharacters.None),
                               Factory.Code(" var foo = bar; if(foo != null) { bar(); } ").AsStatement()
                               ),
                           new RazorError(
                               RazorResources.FormatParseError_Expected_EndOfBlock_Before_EOF(RazorResources.BlockName_Code, '}', '{'),
                               SourceLocation.Zero));
        }

        [Fact]
        public void ParseBlockReportsErrorIfClassBlockUnterminatedAtEOF()
        {
            ParseBlockTest("functions { var foo = bar; if(foo != null) { bar(); } ",
                           new FunctionsBlock(
                               Factory.MetaCode("functions {").Accepts(AcceptedCharacters.None),
                               Factory.Code(" var foo = bar; if(foo != null) { bar(); } ").AsFunctionsBody()
                               ),
                           new RazorError(
                               RazorResources.FormatParseError_Expected_EndOfBlock_Before_EOF("functions", '}', '{'),
                               SourceLocation.Zero));
        }

        [Fact]
        public void ParseBlockReportsErrorIfIfBlockUnterminatedAtEOF()
        {
            RunUnterminatedSimpleKeywordBlock("if");
        }

        [Fact]
        public void ParseBlockReportsErrorIfElseBlockUnterminatedAtEOF()
        {
            ParseBlockTest("if(foo) { baz(); } else { var foo = bar; if(foo != null) { bar(); } ",
                           new StatementBlock(
                               Factory.Code("if(foo) { baz(); } else { var foo = bar; if(foo != null) { bar(); } ").AsStatement()
                               ),
                           new RazorError(
                               RazorResources.FormatParseError_Expected_EndOfBlock_Before_EOF("else", '}', '{'),
                               new SourceLocation(19, 0, 19)));
        }

        [Fact]
        public void ParseBlockReportsErrorIfElseIfBlockUnterminatedAtEOF()
        {
            ParseBlockTest("if(foo) { baz(); } else if { var foo = bar; if(foo != null) { bar(); } ",
                           new StatementBlock(
                               Factory.Code("if(foo) { baz(); } else if { var foo = bar; if(foo != null) { bar(); } ").AsStatement()
                               ),
                           new RazorError(
                               RazorResources.FormatParseError_Expected_EndOfBlock_Before_EOF("else if", '}', '{'),
                               new SourceLocation(19, 0, 19)));
        }

        [Fact]
        public void ParseBlockReportsErrorIfDoBlockUnterminatedAtEOF()
        {
            ParseBlockTest("do { var foo = bar; if(foo != null) { bar(); } ",
                           new StatementBlock(
                               Factory.Code("do { var foo = bar; if(foo != null) { bar(); } ").AsStatement()
                               ),
                           new RazorError(
                               RazorResources.FormatParseError_Expected_EndOfBlock_Before_EOF("do", '}', '{'),
                               SourceLocation.Zero));
        }

        [Fact]
        public void ParseBlockReportsErrorIfTryBlockUnterminatedAtEOF()
        {
            ParseBlockTest("try { var foo = bar; if(foo != null) { bar(); } ",
                           new StatementBlock(
                               Factory.Code("try { var foo = bar; if(foo != null) { bar(); } ").AsStatement()
                               ),
                           new RazorError(
                               RazorResources.FormatParseError_Expected_EndOfBlock_Before_EOF("try", '}', '{'),
                               SourceLocation.Zero));
        }

        [Fact]
        public void ParseBlockReportsErrorIfCatchBlockUnterminatedAtEOF()
        {
            ParseBlockTest("try { baz(); } catch(Foo) { var foo = bar; if(foo != null) { bar(); } ",
                           new StatementBlock(
                               Factory.Code("try { baz(); } catch(Foo) { var foo = bar; if(foo != null) { bar(); } ").AsStatement()
                               ),
                           new RazorError(
                               RazorResources.FormatParseError_Expected_EndOfBlock_Before_EOF("catch", '}', '{'),
                               new SourceLocation(15, 0, 15)));
        }

        [Fact]
        public void ParseBlockReportsErrorIfFinallyBlockUnterminatedAtEOF()
        {
            ParseBlockTest("try { baz(); } finally { var foo = bar; if(foo != null) { bar(); } ",
                           new StatementBlock(
                               Factory.Code("try { baz(); } finally { var foo = bar; if(foo != null) { bar(); } ").AsStatement()
                               ),
                           new RazorError(
                               RazorResources.FormatParseError_Expected_EndOfBlock_Before_EOF("finally", '}', '{'),
                               new SourceLocation(15, 0, 15)));
        }

        [Fact]
        public void ParseBlockReportsErrorIfForBlockUnterminatedAtEOF()
        {
            RunUnterminatedSimpleKeywordBlock("for");
        }

        [Fact]
        public void ParseBlockReportsErrorIfForeachBlockUnterminatedAtEOF()
        {
            RunUnterminatedSimpleKeywordBlock("foreach");
        }

        [Fact]
        public void ParseBlockReportsErrorIfWhileBlockUnterminatedAtEOF()
        {
            RunUnterminatedSimpleKeywordBlock("while");
        }

        [Fact]
        public void ParseBlockReportsErrorIfSwitchBlockUnterminatedAtEOF()
        {
            RunUnterminatedSimpleKeywordBlock("switch");
        }

        [Fact]
        public void ParseBlockReportsErrorIfLockBlockUnterminatedAtEOF()
        {
            RunUnterminatedSimpleKeywordBlock("lock");
        }

        [Fact]
        public void ParseBlockReportsErrorIfUsingBlockUnterminatedAtEOF()
        {
            RunUnterminatedSimpleKeywordBlock("using");
        }

        [Fact]
        public void ParseBlockRequiresControlFlowStatementsToHaveBraces()
        {
            string expectedMessage = RazorResources.FormatParseError_SingleLine_ControlFlowStatements_Not_Allowed("{", "<");
            ParseBlockTest("if(foo) <p>Bar</p> else if(bar) <p>Baz</p> else <p>Boz</p>",
                           new StatementBlock(
                               Factory.Code("if(foo) ").AsStatement(),
                               new MarkupBlock(
                                   Factory.Markup("<p>Bar</p> ").Accepts(AcceptedCharacters.None)
                                   ),
                               Factory.Code("else if(bar) ").AsStatement(),
                               new MarkupBlock(
                                   Factory.Markup("<p>Baz</p> ").Accepts(AcceptedCharacters.None)
                                   ),
                               Factory.Code("else ").AsStatement(),
                               new MarkupBlock(
                                   Factory.Markup("<p>Boz</p>").Accepts(AcceptedCharacters.None)
                                   ),
                               Factory.EmptyCSharp().AsStatement()
                               ),
                           new RazorError(expectedMessage, 8, 0, 8),
                           new RazorError(expectedMessage, 32, 0, 32),
                           new RazorError(expectedMessage, 48, 0, 48));
        }

        [Fact]
        public void ParseBlockIncludesUnexpectedCharacterInSingleStatementControlFlowStatementError()
        {
            ParseBlockTest("if(foo)) { var bar = foo; }",
                           new StatementBlock(
                               Factory.Code("if(foo)) { var bar = foo; }").AsStatement()
                               ),
                           new RazorError(
                               RazorResources.FormatParseError_SingleLine_ControlFlowStatements_Not_Allowed("{", ")"),
                               new SourceLocation(7, 0, 7)));
        }

        [Fact]
        public void ParseBlockOutputsErrorIfAtSignFollowedByLessThanSignAtStatementStart()
        {
            ParseBlockTest("if(foo) { @<p>Bar</p> }",
                           new StatementBlock(
                               Factory.Code("if(foo) {").AsStatement(),
                               new MarkupBlock(
                                   Factory.Markup(" "),
                                   Factory.MarkupTransition(),
                                   Factory.Markup("<p>Bar</p> ").Accepts(AcceptedCharacters.None)
                                   ),
                               Factory.Code("}").AsStatement()
                               ),
                           new RazorError(
                               RazorResources.ParseError_AtInCode_Must_Be_Followed_By_Colon_Paren_Or_Identifier_Start,
                               10, 0, 10));
        }

        [Fact]
        public void ParseBlockTerminatesIfBlockAtEOLWhenRecoveringFromMissingCloseParen()
        {
            ParseBlockTest("if(foo bar" + Environment.NewLine
                         + "baz",
                           new StatementBlock(
                               Factory.Code("if(foo bar\r\n").AsStatement()
                               ),
                           new RazorError(
                               RazorResources.FormatParseError_Expected_CloseBracket_Before_EOF("(", ")"),
                               new SourceLocation(2, 0, 2)));
        }

        [Fact]
        public void ParseBlockTerminatesForeachBlockAtEOLWhenRecoveringFromMissingCloseParen()
        {
            ParseBlockTest("foreach(foo bar" + Environment.NewLine
                         + "baz",
                           new StatementBlock(
                               Factory.Code("foreach(foo bar\r\n").AsStatement()
                               ),
                           new RazorError(
                               RazorResources.FormatParseError_Expected_CloseBracket_Before_EOF("(", ")"),
                               new SourceLocation(7, 0, 7)));
        }

        [Fact]
        public void ParseBlockTerminatesWhileClauseInDoStatementAtEOLWhenRecoveringFromMissingCloseParen()
        {
            ParseBlockTest("do { } while(foo bar" + Environment.NewLine
                         + "baz",
                           new StatementBlock(
                               Factory.Code("do { } while(foo bar\r\n").AsStatement()
                               ),
                           new RazorError(
                               RazorResources.FormatParseError_Expected_CloseBracket_Before_EOF("(", ")"),
                               new SourceLocation(12, 0, 12)));
        }

        [Fact]
        public void ParseBlockTerminatesUsingBlockAtEOLWhenRecoveringFromMissingCloseParen()
        {
            ParseBlockTest("using(foo bar" + Environment.NewLine
                         + "baz",
                           new StatementBlock(
                               Factory.Code("using(foo bar\r\n").AsStatement()
                               ),
                           new RazorError(
                               RazorResources.FormatParseError_Expected_CloseBracket_Before_EOF("(", ")"),
                               new SourceLocation(5, 0, 5)));
        }

        [Fact]
        public void ParseBlockResumesIfStatementAfterOpenParen()
        {
            ParseBlockTest("if(" + Environment.NewLine
                         + "else { <p>Foo</p> }",
                           new StatementBlock(
                               Factory.Code("if(\r\nelse {").AsStatement(),
                               new MarkupBlock(
                                   Factory.Markup(" <p>Foo</p> ").Accepts(AcceptedCharacters.None)
                                   ),
                               Factory.Code("}").AsStatement().Accepts(AcceptedCharacters.None)
                               ),
                           new RazorError(
                               RazorResources.FormatParseError_Expected_CloseBracket_Before_EOF("(", ")"),
                               new SourceLocation(2, 0, 2)));
        }

        [Fact]
        public void ParseBlockTerminatesNormalCSharpStringsAtEOLIfEndQuoteMissing()
        {
            SingleSpanBlockTest("if(foo) {" + Environment.NewLine
                              + "    var p = \"foo bar baz" + Environment.NewLine
                              + ";" + Environment.NewLine
                              + "}",
                                BlockType.Statement, SpanKind.Code,
                                new RazorError(RazorResources.ParseError_Unterminated_String_Literal, 23, 1, 12));
        }

        [Fact]
        public void ParseBlockTerminatesNormalStringAtEndOfFile()
        {
            SingleSpanBlockTest("if(foo) { var foo = \"blah blah blah blah blah", BlockType.Statement, SpanKind.Code,
                                new RazorError(RazorResources.ParseError_Unterminated_String_Literal, 20, 0, 20),
                                new RazorError(RazorResources.FormatParseError_Expected_EndOfBlock_Before_EOF("if", '}', '{'), SourceLocation.Zero));
        }

        [Fact]
        public void ParseBlockTerminatesVerbatimStringAtEndOfFile()
        {
            SingleSpanBlockTest("if(foo) { var foo = @\"blah " + Environment.NewLine
                              + "blah; " + Environment.NewLine
                              + "<p>Foo</p>" + Environment.NewLine
                              + "blah " + Environment.NewLine
                              + "blah",
                                BlockType.Statement, SpanKind.Code,
                                new RazorError(RazorResources.ParseError_Unterminated_String_Literal, 20, 0, 20),
                                new RazorError(RazorResources.FormatParseError_Expected_EndOfBlock_Before_EOF("if", '}', '{'), SourceLocation.Zero));
        }

        [Fact]
        public void ParseBlockCorrectlyParsesMarkupIncorrectyAssumedToBeWithinAStatement()
        {
            ParseBlockTest("if(foo) {" + Environment.NewLine
                         + "    var foo = \"foo bar baz" + Environment.NewLine
                         + "    <p>Foo is @foo</p>" + Environment.NewLine
                         + "}",
                           new StatementBlock(
                               Factory.Code("if(foo) {\r\n    var foo = \"foo bar baz\r\n    ").AsStatement(),
                               new MarkupBlock(
                                   Factory.Markup("<p>Foo is "),
                                   new ExpressionBlock(
                                       Factory.CodeTransition(),
                                       Factory.Code("foo")
                                           .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                                           .Accepts(AcceptedCharacters.NonWhiteSpace)),
                                   Factory.Markup("</p>\r\n").Accepts(AcceptedCharacters.None)),
                               Factory.Code("}").AsStatement()
                               ),
                           new RazorError(
                               RazorResources.ParseError_Unterminated_String_Literal,
                               25, 1, 14));
        }

        [Fact]
        public void ParseBlockCorrectlyParsesAtSignInDelimitedBlock()
        {
            ParseBlockTest("(Request[\"description\"] ?? @photo.Description)",
                           new ExpressionBlock(
                               Factory.MetaCode("(").Accepts(AcceptedCharacters.None),
                               Factory.Code("Request[\"description\"] ?? @photo.Description").AsExpression(),
                               Factory.MetaCode(")").Accepts(AcceptedCharacters.None)
                               ));
        }

        [Fact]
        public void ParseBlockCorrectlyRecoversFromMissingCloseParenInExpressionWithinCode()
        {
            ParseBlockTest(@"{String.Format(<html></html>}",
                new StatementBlock(
                    Factory.MetaCode("{").Accepts(AcceptedCharacters.None),
                    Factory.Code("String.Format(")
                           .AsStatement(),
                    new MarkupBlock(
                        Factory.Markup("<html></html>").Accepts(AcceptedCharacters.None)),
                    Factory.EmptyCSharp().AsStatement(),
                    Factory.MetaCode("}").Accepts(AcceptedCharacters.None)),
                expectedErrors: new[] {
                    new RazorError(
                        RazorResources.FormatParseError_Expected_CloseBracket_Before_EOF("(", ")"),
                        14, 0, 14)
                });

        }

        private void RunUnterminatedSimpleKeywordBlock(string keyword)
        {
            SingleSpanBlockTest(keyword + " (foo) { var foo = bar; if(foo != null) { bar(); } ", BlockType.Statement, SpanKind.Code,
                                new RazorError(RazorResources.FormatParseError_Expected_EndOfBlock_Before_EOF(keyword, '}', '{'), SourceLocation.Zero));
        }
    }
}
