// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language.Extensions;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
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
                                   .Accepts(AcceptedCharactersInternal.NonWhiteSpace)
                               ),
                           RazorDiagnosticFactory.CreateParsing_UnexpectedCharacterAtStartOfCodeBlock(
                                new SourceSpan(new SourceLocation(1, 0, 1), contentLength: 1),
                                "\""));
        }

        [Fact]
        public void ParseBlockWithHelperDirectiveProducesError()
        {
            ParseBlockTest("@helper fooHelper { }",
                new ExpressionBlock(
                    Factory.CodeTransition(),
                    Factory.Code("helper")
                        .AsImplicitExpression(KeywordSet)
                        .Accepts(AcceptedCharactersInternal.NonWhiteSpace)),
                RazorDiagnosticFactory.CreateParsing_HelperDirectiveNotAvailable(
                    new SourceSpan(new SourceLocation(1, 0, 1), contentLength: 6)));
        }

        [Fact]
        public void ParseBlockWithNestedCodeBlockProducesError()
        {
            ParseBlockTest("@if { @{} }",
                new StatementBlock(
                    Factory.CodeTransition(),
                    Factory.Code("if { ")
                        .AsStatement()
                        .Accepts(AcceptedCharactersInternal.Any),
                    new StatementBlock(
                        Factory.CodeTransition(),
                        Factory.MetaCode("{").Accepts(AcceptedCharactersInternal.None),
                        Factory.EmptyCSharp()
                            .AsStatement()
                            .AutoCompleteWith(autoCompleteString: null),
                        Factory.MetaCode("}").Accepts(AcceptedCharactersInternal.None)),
                    Factory.Code(" }")
                        .AsStatement()
                        .Accepts(AcceptedCharactersInternal.Any)),
                RazorDiagnosticFactory.CreateParsing_UnexpectedNestedCodeBlock(
                    new SourceSpan(new SourceLocation(7, 0, 7), contentLength: 1)));
        }

        [Fact]
        public void ParseBlockCapturesWhitespaceToEndOfLineInInvalidUsingStatementAndTreatsAsFileCode()
        {
            ParseBlockTest("using          " + Environment.NewLine
                         + Environment.NewLine,
                           new StatementBlock(
                               Factory.Code("using          " + Environment.NewLine).AsStatement()
                               ));
        }

        [Fact]
        public void ParseBlockMethodOutputsOpenCurlyAsCodeSpanIfEofFoundAfterOpenCurlyBrace()
        {
            ParseBlockTest("{",
                           new StatementBlock(
                               Factory.MetaCode("{").Accepts(AcceptedCharactersInternal.None),
                               Factory.EmptyCSharp()
                                   .AsStatement()
                                   .With(new AutoCompleteEditHandler(CSharpLanguageCharacteristics.Instance.TokenizeString) { AutoCompleteString = "}" })
                               ),
                           RazorDiagnosticFactory.CreateParsing_ExpectedEndOfBlockBeforeEOF(
                                new SourceSpan(SourceLocation.Zero, contentLength: 1), LegacyResources.BlockName_Code, "}", "{"));
        }

        [Fact]
        public void ParseBlockMethodOutputsZeroLengthCodeSpanIfStatementBlockEmpty()
        {
            ParseBlockTest("{}",
                           new StatementBlock(
                               Factory.MetaCode("{").Accepts(AcceptedCharactersInternal.None),
                               Factory.EmptyCSharp()
                                   .AsStatement()
                                   .AutoCompleteWith(autoCompleteString: null),
                               Factory.MetaCode("}").Accepts(AcceptedCharactersInternal.None)
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
                                   .Accepts(AcceptedCharactersInternal.NonWhiteSpace)),
                           RazorDiagnosticFactory.CreateParsing_UnexpectedWhiteSpaceAtStartOfCodeBlock(
                                new SourceSpan(new SourceLocation(1, 0, 1), Environment.NewLine.Length)));
        }

        [Fact]
        public void ParseBlockMethodProducesErrorIfWhitespaceBetweenTransitionAndBlockStartInEmbeddedExpression()
        {
            ParseBlockTest("{" + Environment.NewLine
                         + "    @   {}" + Environment.NewLine
                         + "}",
                           new StatementBlock(
                               Factory.MetaCode("{").Accepts(AcceptedCharactersInternal.None),
                               Factory.Code(Environment.NewLine + "    ")
                                   .AsStatement()
                                   .AutoCompleteWith(autoCompleteString: null),
                               new ExpressionBlock(
                                   Factory.CodeTransition(),
                                   Factory.EmptyCSharp()
                                       .AsImplicitExpression(CSharpCodeParser.DefaultKeywords, acceptTrailingDot: true)
                                       .Accepts(AcceptedCharactersInternal.NonWhiteSpace)),
                               Factory.Code("   {}" + Environment.NewLine).AsStatement(),
                               Factory.MetaCode("}").Accepts(AcceptedCharactersInternal.None)
                               ),
                           RazorDiagnosticFactory.CreateParsing_UnexpectedWhiteSpaceAtStartOfCodeBlock(
                                new SourceSpan(new SourceLocation(6 + Environment.NewLine.Length, 1, 5), contentLength: 3)));
        }

        [Fact]
        public void ParseBlockMethodProducesErrorIfEOFAfterTransitionInEmbeddedExpression()
        {
            ParseBlockTest("{" + Environment.NewLine
                         + "    @",
                           new StatementBlock(
                               Factory.MetaCode("{").Accepts(AcceptedCharactersInternal.None),
                               Factory.Code(Environment.NewLine + "    ")
                                   .AsStatement()
                                   .AutoCompleteWith("}"),
                               new ExpressionBlock(
                                   Factory.CodeTransition(),
                                   Factory.EmptyCSharp()
                                       .AsImplicitExpression(CSharpCodeParser.DefaultKeywords, acceptTrailingDot: true)
                                       .Accepts(AcceptedCharactersInternal.NonWhiteSpace)),
                               Factory.EmptyCSharp().AsStatement()
                               ),
                           RazorDiagnosticFactory.CreateParsing_UnexpectedEndOfFileAtStartOfCodeBlock(
                               new SourceSpan(new SourceLocation(6 + Environment.NewLine.Length, 1, 5), contentLength: 1)),
                           RazorDiagnosticFactory.CreateParsing_ExpectedEndOfBlockBeforeEOF(
                               new SourceSpan(SourceLocation.Zero, contentLength: 1), LegacyResources.BlockName_Code, "}", "{"));
        }

        [Fact]
        public void ParseBlockMethodParsesNothingIfFirstCharacterIsNotIdentifierStartOrParenOrBrace()
        {
            ParseBlockTest("@!!!",
                           new ExpressionBlock(
                               Factory.CodeTransition(),
                               Factory.EmptyCSharp()
                                   .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                                   .Accepts(AcceptedCharactersInternal.NonWhiteSpace)),
                           RazorDiagnosticFactory.CreateParsing_UnexpectedCharacterAtStartOfCodeBlock(
                                new SourceSpan(new SourceLocation(1, 0, 1), contentLength: 1),
                                "!"));
        }

        [Fact]
        public void ParseBlockShouldReportErrorAndTerminateAtEOFIfIfParenInExplicitExpressionUnclosed()
        {
            ParseBlockTest("(foo bar" + Environment.NewLine
                         + "baz",
                           new ExpressionBlock(
                               Factory.MetaCode("(").Accepts(AcceptedCharactersInternal.None),
                               Factory.Code($"foo bar{Environment.NewLine}baz").AsExpression()
                               ),
                           RazorDiagnosticFactory.CreateParsing_ExpectedEndOfBlockBeforeEOF(
                            new SourceSpan(SourceLocation.Zero, contentLength: 1), LegacyResources.BlockName_ExplicitExpression, ")", "("));
        }

        [Fact]
        public void ParseBlockShouldReportErrorAndTerminateAtMarkupIfIfParenInExplicitExpressionUnclosed()
        {
            ParseBlockTest("(foo bar" + Environment.NewLine
                         + "<html>" + Environment.NewLine
                         + "baz" + Environment.NewLine
                         + "</html",
                           new ExpressionBlock(
                               Factory.MetaCode("(").Accepts(AcceptedCharactersInternal.None),
                               Factory.Code($"foo bar{Environment.NewLine}").AsExpression()
                               ),
                           RazorDiagnosticFactory.CreateParsing_ExpectedEndOfBlockBeforeEOF(
                            new SourceSpan(SourceLocation.Zero, contentLength: 1), LegacyResources.BlockName_ExplicitExpression, ")", "("));
        }

        [Fact]
        public void ParseBlockCorrectlyHandlesInCorrectTransitionsIfImplicitExpressionParensUnclosed()
        {
            ParseBlockTest("Href(" + Environment.NewLine
                         + "<h1>@Html.Foo(Bar);</h1>" + Environment.NewLine,
                           new ExpressionBlock(
                               Factory.Code("Href(" + Environment.NewLine)
                                   .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                               ),
                           RazorDiagnostic.Create(new RazorError(
                               LegacyResources.FormatParseError_Expected_CloseBracket_Before_EOF("(", ")"),
                               new SourceLocation(4, 0, 4),
                               length: 1)));
        }

        [Fact]
        // Test for fix to Dev10 884975 - Incorrect Error Messaging
        public void ParseBlockShouldReportErrorAndTerminateAtEOFIfParenInImplicitExpressionUnclosed()
        {
            ParseBlockTest("Foo(Bar(Baz)" + Environment.NewLine
                            + "Biz" + Environment.NewLine
                            + "Boz",
                            new ExpressionBlock(
                                Factory.Code($"Foo(Bar(Baz){Environment.NewLine}Biz{Environment.NewLine}Boz")
                                    .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                                ),
                            RazorDiagnostic.Create(new RazorError(
                                LegacyResources.FormatParseError_Expected_CloseBracket_Before_EOF("(", ")"),
                                new SourceLocation(3, 0, 3),
                                length: 1)));
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
                                Factory.Code($"Foo(Bar(Baz){Environment.NewLine}Biz{Environment.NewLine}")
                                    .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                                ),
                            RazorDiagnostic.Create(new RazorError(
                                LegacyResources.FormatParseError_Expected_CloseBracket_Before_EOF("(", ")"),
                                new SourceLocation(3, 0, 3),
                               length: 1)));
        }

        [Fact]
        // Test for fix to Dev10 884975 - Incorrect Error Messaging
        public void ParseBlockShouldReportErrorAndTerminateAtEOFIfBracketInImplicitExpressionUnclosed()
        {
            ParseBlockTest("Foo[Bar[Baz]" + Environment.NewLine
                         + "Biz" + Environment.NewLine
                         + "Boz",
                           new ExpressionBlock(
                               Factory.Code($"Foo[Bar[Baz]{Environment.NewLine}Biz{Environment.NewLine}Boz")
                                   .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                               ),
                           RazorDiagnostic.Create(new RazorError(
                               LegacyResources.FormatParseError_Expected_CloseBracket_Before_EOF("[", "]"),
                               new SourceLocation(3, 0, 3),
                               length: 1)));
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
                               Factory.Code($"Foo[Bar[Baz]{Environment.NewLine}Biz{Environment.NewLine}")
                                   .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                               ),
                           RazorDiagnostic.Create(new RazorError(
                               LegacyResources.FormatParseError_Expected_CloseBracket_Before_EOF("[", "]"),
                               new SourceLocation(3, 0, 3),
                               length: 1)));
        }

        // Simple EOF handling errors:
        [Fact]
        public void ParseBlockReportsErrorIfExplicitCodeBlockUnterminatedAtEOF()
        {
            ParseBlockTest("{ var foo = bar; if(foo != null) { bar(); } ",
                           new StatementBlock(
                               Factory.MetaCode("{").Accepts(AcceptedCharactersInternal.None),
                               Factory.Code(" var foo = bar; if(foo != null) { bar(); } ")
                                   .AsStatement()
                                   .AutoCompleteWith("}")),
                           RazorDiagnosticFactory.CreateParsing_ExpectedEndOfBlockBeforeEOF(
                               new SourceSpan(SourceLocation.Zero, contentLength: 1), LegacyResources.BlockName_Code, "}", "{"));
        }

        [Fact]
        public void ParseBlockReportsErrorIfClassBlockUnterminatedAtEOF()
        {
            // Arrange
            var chunkGenerator = new DirectiveChunkGenerator(FunctionsDirective.Directive);
            chunkGenerator.Diagnostics.Add(
                RazorDiagnosticFactory.CreateParsing_ExpectedEndOfBlockBeforeEOF(
                    new SourceSpan(new SourceLocation(10, 0, 10), contentLength: 1), "functions", "}", "{"));

            // Act & Assert
            ParseBlockTest(
                "functions { var foo = bar; if(foo != null) { bar(); } ",
                new[] { FunctionsDirective.Directive },
                new DirectiveBlock(chunkGenerator,
                    Factory.MetaCode("functions").Accepts(AcceptedCharactersInternal.None),
                    Factory.Span(SpanKindInternal.Markup, " ", CSharpSymbolType.WhiteSpace).Accepts(AcceptedCharactersInternal.AllWhiteSpace),
                    Factory.MetaCode("{").AutoCompleteWith("}", atEndOfSpan: true).Accepts(AcceptedCharactersInternal.None),
                    Factory.Code(" var foo = bar; if(foo != null) { bar(); } ").AsStatement()));
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
                           RazorDiagnosticFactory.CreateParsing_ExpectedEndOfBlockBeforeEOF(
                               new SourceSpan(new SourceLocation(19, 0, 19), contentLength: 1), "else", "}", "{"));
        }

        [Fact]
        public void ParseBlockReportsErrorIfElseIfBlockUnterminatedAtEOF()
        {
            ParseBlockTest("if(foo) { baz(); } else if { var foo = bar; if(foo != null) { bar(); } ",
                           new StatementBlock(
                               Factory.Code("if(foo) { baz(); } else if { var foo = bar; if(foo != null) { bar(); } ").AsStatement()
                               ),
                           RazorDiagnosticFactory.CreateParsing_ExpectedEndOfBlockBeforeEOF(
                               new SourceSpan(new SourceLocation(19, 0, 19), contentLength: 1), "else if", "}", "{"));
        }

        [Fact]
        public void ParseBlockReportsErrorIfDoBlockUnterminatedAtEOF()
        {
            ParseBlockTest("do { var foo = bar; if(foo != null) { bar(); } ",
                           new StatementBlock(
                               Factory.Code("do { var foo = bar; if(foo != null) { bar(); } ").AsStatement()
                               ),
                           RazorDiagnosticFactory.CreateParsing_ExpectedEndOfBlockBeforeEOF(
                               new SourceSpan(SourceLocation.Zero, contentLength: 1), "do", "}", "{"));
        }

        [Fact]
        public void ParseBlockReportsErrorIfTryBlockUnterminatedAtEOF()
        {
            ParseBlockTest("try { var foo = bar; if(foo != null) { bar(); } ",
                           new StatementBlock(
                               Factory.Code("try { var foo = bar; if(foo != null) { bar(); } ").AsStatement()
                               ),
                           RazorDiagnosticFactory.CreateParsing_ExpectedEndOfBlockBeforeEOF(
                               new SourceSpan(SourceLocation.Zero, contentLength: 1), "try", "}", "{"));
        }

        [Fact]
        public void ParseBlockReportsErrorIfCatchBlockUnterminatedAtEOF()
        {
            ParseBlockTest("try { baz(); } catch(Foo) { var foo = bar; if(foo != null) { bar(); } ",
                           new StatementBlock(
                               Factory.Code("try { baz(); } catch(Foo) { var foo = bar; if(foo != null) { bar(); } ").AsStatement()
                               ),
                           RazorDiagnosticFactory.CreateParsing_ExpectedEndOfBlockBeforeEOF(
                               new SourceSpan(new SourceLocation(15, 0, 15), contentLength: 1), "catch", "}", "{"));
        }

        [Fact]
        public void ParseBlockReportsErrorIfFinallyBlockUnterminatedAtEOF()
        {
            ParseBlockTest("try { baz(); } finally { var foo = bar; if(foo != null) { bar(); } ",
                           new StatementBlock(
                               Factory.Code("try { baz(); } finally { var foo = bar; if(foo != null) { bar(); } ").AsStatement()
                               ),
                           RazorDiagnosticFactory.CreateParsing_ExpectedEndOfBlockBeforeEOF(
                               new SourceSpan(new SourceLocation(15, 0, 15), contentLength: 1), "finally", "}", "{"));
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
            var expectedMessage = LegacyResources.FormatParseError_SingleLine_ControlFlowStatements_Not_Allowed("{", "<");
            ParseBlockTest("if(foo) <p>Bar</p> else if(bar) <p>Baz</p> else <p>Boz</p>",
                           new StatementBlock(
                               Factory.Code("if(foo) ").AsStatement(),
                               new MarkupBlock(
                                    BlockFactory.MarkupTagBlock("<p>", AcceptedCharactersInternal.None),
                                    Factory.Markup("Bar"),
                                    BlockFactory.MarkupTagBlock("</p>", AcceptedCharactersInternal.None),
                                    Factory.Markup(" ").Accepts(AcceptedCharactersInternal.None)),
                               Factory.Code("else if(bar) ").AsStatement(),
                               new MarkupBlock(
                                    BlockFactory.MarkupTagBlock("<p>", AcceptedCharactersInternal.None),
                                    Factory.Markup("Baz"),
                                    BlockFactory.MarkupTagBlock("</p>", AcceptedCharactersInternal.None),
                                    Factory.Markup(" ").Accepts(AcceptedCharactersInternal.None)),
                               Factory.Code("else ").AsStatement(),
                               new MarkupBlock(
                                    BlockFactory.MarkupTagBlock("<p>", AcceptedCharactersInternal.None),
                                    Factory.Markup("Boz"),
                                    BlockFactory.MarkupTagBlock("</p>", AcceptedCharactersInternal.None)),
                               Factory.EmptyCSharp().AsStatement()
                               ),
                           RazorDiagnosticFactory.CreateParsing_SingleLineControlFlowStatementsNotAllowed(
                               new SourceSpan(new SourceLocation(8, 0, 8), contentLength: 1), "{", "<"),
                           RazorDiagnosticFactory.CreateParsing_SingleLineControlFlowStatementsNotAllowed(
                               new SourceSpan(new SourceLocation(32, 0, 32), contentLength: 1), "{", "<"),
                           RazorDiagnosticFactory.CreateParsing_SingleLineControlFlowStatementsNotAllowed(
                               new SourceSpan(new SourceLocation(48, 0, 48), contentLength: 1), "{", "<"));
        }

        [Fact]
        public void ParseBlockIncludesUnexpectedCharacterInSingleStatementControlFlowStatementError()
        {
            ParseBlockTest("if(foo)) { var bar = foo; }",
                           new StatementBlock(
                               Factory.Code("if(foo)) { var bar = foo; }").AsStatement()
                               ),
                           RazorDiagnosticFactory.CreateParsing_SingleLineControlFlowStatementsNotAllowed(
                               new SourceSpan(new SourceLocation(7, 0, 7), contentLength: 1), "{", ")"));
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
                                   BlockFactory.MarkupTagBlock("<p>", AcceptedCharactersInternal.None),
                                    Factory.Markup("Bar"),
                                    BlockFactory.MarkupTagBlock("</p>", AcceptedCharactersInternal.None),
                                    Factory.Markup(" ").Accepts(AcceptedCharactersInternal.None)),
                               Factory.Code("}").AsStatement()
                               ),
                           RazorDiagnosticFactory.CreateParsing_AtInCodeMustBeFollowedByColonParenOrIdentifierStart(
                               new SourceSpan(new SourceLocation(10, 0, 10), contentLength: 1)));
        }

        [Fact]
        public void ParseBlockTerminatesIfBlockAtEOLWhenRecoveringFromMissingCloseParen()
        {
            ParseBlockTest("if(foo bar" + Environment.NewLine
                         + "baz",
                           new StatementBlock(
                               Factory.Code("if(foo bar" + Environment.NewLine).AsStatement()
                               ),
                           RazorDiagnostic.Create(new RazorError(
                               LegacyResources.FormatParseError_Expected_CloseBracket_Before_EOF("(", ")"),
                               new SourceLocation(2, 0, 2),
                               length: 1)));
        }

        [Fact]
        public void ParseBlockTerminatesForeachBlockAtEOLWhenRecoveringFromMissingCloseParen()
        {
            ParseBlockTest("foreach(foo bar" + Environment.NewLine
                         + "baz",
                           new StatementBlock(
                               Factory.Code("foreach(foo bar" + Environment.NewLine).AsStatement()
                               ),
                           RazorDiagnostic.Create(new RazorError(
                               LegacyResources.FormatParseError_Expected_CloseBracket_Before_EOF("(", ")"),
                               new SourceLocation(7, 0, 7),
                               length: 1)));
        }

        [Fact]
        public void ParseBlockTerminatesWhileClauseInDoStatementAtEOLWhenRecoveringFromMissingCloseParen()
        {
            ParseBlockTest("do { } while(foo bar" + Environment.NewLine
                         + "baz",
                           new StatementBlock(
                               Factory.Code("do { } while(foo bar" + Environment.NewLine).AsStatement()
                               ),
                           RazorDiagnostic.Create(new RazorError(
                               LegacyResources.FormatParseError_Expected_CloseBracket_Before_EOF("(", ")"),
                               new SourceLocation(12, 0, 12),
                               length: 1)));
        }

        [Fact]
        public void ParseBlockTerminatesUsingBlockAtEOLWhenRecoveringFromMissingCloseParen()
        {
            ParseBlockTest("using(foo bar" + Environment.NewLine
                         + "baz",
                           new StatementBlock(
                               Factory.Code("using(foo bar" + Environment.NewLine).AsStatement()
                               ),
                           RazorDiagnostic.Create(new RazorError(
                               LegacyResources.FormatParseError_Expected_CloseBracket_Before_EOF("(", ")"),
                               new SourceLocation(5, 0, 5),
                               length: 1)));
        }

        [Fact]
        public void ParseBlockResumesIfStatementAfterOpenParen()
        {
            ParseBlockTest("if(" + Environment.NewLine
                         + "else { <p>Foo</p> }",
                           new StatementBlock(
                               Factory.Code($"if({Environment.NewLine}else {{").AsStatement(),
                               new MarkupBlock(
                                   Factory.Markup(" "),
                                    BlockFactory.MarkupTagBlock("<p>", AcceptedCharactersInternal.None),
                                    Factory.Markup("Foo"),
                                    BlockFactory.MarkupTagBlock("</p>", AcceptedCharactersInternal.None),
                                    Factory.Markup(" ").Accepts(AcceptedCharactersInternal.None)),
                               Factory.Code("}").AsStatement().Accepts(AcceptedCharactersInternal.None)
                               ),
                           RazorDiagnostic.Create(new RazorError(
                               LegacyResources.FormatParseError_Expected_CloseBracket_Before_EOF("(", ")"),
                               new SourceLocation(2, 0, 2),
                               length: 1)));
        }

        [Fact]
        public void ParseBlockTerminatesNormalCSharpStringsAtEOLIfEndQuoteMissing()
        {
            SingleSpanBlockTest("if(foo) {" + Environment.NewLine
                              + "    var p = \"foo bar baz" + Environment.NewLine
                              + ";" + Environment.NewLine
                              + "}",
                                BlockKindInternal.Statement, SpanKindInternal.Code,
                                RazorDiagnosticFactory.CreateParsing_UnterminatedStringLiteral(
                                    new SourceSpan(new SourceLocation(21 + Environment.NewLine.Length, 1, 12), contentLength: 1)));
        }

        [Fact]
        public void ParseBlockTerminatesNormalStringAtEndOfFile()
        {
            SingleSpanBlockTest("if(foo) { var foo = \"blah blah blah blah blah", BlockKindInternal.Statement, SpanKindInternal.Code,
                                RazorDiagnosticFactory.CreateParsing_UnterminatedStringLiteral(
                                    new SourceSpan(new SourceLocation(20, 0, 20), contentLength: 1)),
                                RazorDiagnosticFactory.CreateParsing_ExpectedEndOfBlockBeforeEOF(
                                    new SourceSpan(SourceLocation.Zero, contentLength: 1), "if", "}", "{"));
        }

        [Fact]
        public void ParseBlockTerminatesVerbatimStringAtEndOfFile()
        {
            SingleSpanBlockTest("if(foo) { var foo = @\"blah " + Environment.NewLine
                              + "blah; " + Environment.NewLine
                              + "<p>Foo</p>" + Environment.NewLine
                              + "blah " + Environment.NewLine
                              + "blah",
                                BlockKindInternal.Statement, SpanKindInternal.Code,
                                RazorDiagnosticFactory.CreateParsing_UnterminatedStringLiteral(
                                    new SourceSpan(new SourceLocation(20, 0, 20), contentLength: 1)),
                                RazorDiagnosticFactory.CreateParsing_ExpectedEndOfBlockBeforeEOF(
                                    new SourceSpan(SourceLocation.Zero, contentLength: 1), "if", "}", "{"));
        }

        [Fact]
        public void ParseBlockCorrectlyParsesMarkupIncorrectyAssumedToBeWithinAStatement()
        {
            ParseBlockTest("if(foo) {" + Environment.NewLine
                         + "    var foo = \"foo bar baz" + Environment.NewLine
                         + "    <p>Foo is @foo</p>" + Environment.NewLine
                         + "}",
                           new StatementBlock(
                               Factory.Code($"if(foo) {{{Environment.NewLine}    var foo = \"foo bar baz{Environment.NewLine}    ").AsStatement(),
                               new MarkupBlock(
                                   BlockFactory.MarkupTagBlock("<p>", AcceptedCharactersInternal.None),
                                   Factory.Markup("Foo is "),
                                   new ExpressionBlock(
                                       Factory.CodeTransition(),
                                       Factory.Code("foo")
                                           .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                                           .Accepts(AcceptedCharactersInternal.NonWhiteSpace)),
                                   BlockFactory.MarkupTagBlock("</p>", AcceptedCharactersInternal.None),
                                   Factory.Markup(Environment.NewLine).Accepts(AcceptedCharactersInternal.None)),
                               Factory.Code("}").AsStatement()
                               ),
                           RazorDiagnosticFactory.CreateParsing_UnterminatedStringLiteral(
                               new SourceSpan(new SourceLocation(23 + Environment.NewLine.Length, 1, 14), contentLength: 1)));
        }

        [Fact]
        public void ParseBlockCorrectlyParsesAtSignInDelimitedBlock()
        {
            ParseBlockTest("(Request[\"description\"] ?? @photo.Description)",
                           new ExpressionBlock(
                               Factory.MetaCode("(").Accepts(AcceptedCharactersInternal.None),
                               Factory.Code("Request[\"description\"] ?? @photo.Description").AsExpression(),
                               Factory.MetaCode(")").Accepts(AcceptedCharactersInternal.None)
                               ));
        }

        [Fact]
        public void ParseBlockCorrectlyRecoversFromMissingCloseParenInExpressionWithinCode()
        {
            ParseBlockTest(@"{string.Format(<html></html>}",
                new StatementBlock(
                    Factory.MetaCode("{").Accepts(AcceptedCharactersInternal.None),
                    Factory.Code("string.Format(")
                        .AsStatement()
                        .AutoCompleteWith(autoCompleteString: null),
                    new MarkupBlock(
                        BlockFactory.MarkupTagBlock("<html>", AcceptedCharactersInternal.None),
                        BlockFactory.MarkupTagBlock("</html>", AcceptedCharactersInternal.None)),
                    Factory.EmptyCSharp().AsStatement(),
                    Factory.MetaCode("}").Accepts(AcceptedCharactersInternal.None)),
                expectedErrors: new[]
                {
                    RazorDiagnostic.Create(new RazorError(
                        LegacyResources.FormatParseError_Expected_CloseBracket_Before_EOF("(", ")"),
                        new SourceLocation(14, 0, 14),
                        length: 1)),
                });
        }

        private void RunUnterminatedSimpleKeywordBlock(string keyword)
        {
            SingleSpanBlockTest(
                keyword + " (foo) { var foo = bar; if(foo != null) { bar(); } ",
                BlockKindInternal.Statement,
                SpanKindInternal.Code,
                RazorDiagnosticFactory.CreateParsing_ExpectedEndOfBlockBeforeEOF(
                    new SourceSpan(SourceLocation.Zero, contentLength: 1), keyword, "}", "{"));
        }
    }
}
