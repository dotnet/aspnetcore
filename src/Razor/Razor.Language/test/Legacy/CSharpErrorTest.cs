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
        public void HandlesQuotesAfterTransition()
        {
            ParseBlockTest("@\"");
        }

        [Fact]
        public void WithHelperDirectiveProducesError()
        {
            ParseBlockTest("@helper fooHelper { }");
        }

        [Fact]
        public void WithNestedCodeBlockProducesError()
        {
            ParseBlockTest("@if { @{} }");
        }

        [Fact]
        public void CapturesWhitespaceToEOLInInvalidUsingStmtAndTreatsAsFileCode()
        {
            // ParseBlockCapturesWhitespaceToEndOfLineInInvalidUsingStatementAndTreatsAsFileCode
            ParseBlockTest("using          " + Environment.NewLine
                         + Environment.NewLine);
        }

        [Fact]
        public void MethodOutputsOpenCurlyAsCodeSpanIfEofFoundAfterOpenCurlyBrace()
        {
            ParseBlockTest("{");
        }

        [Fact]
        public void MethodOutputsZeroLengthCodeSpanIfStatementBlockEmpty()
        {
            ParseBlockTest("{}");
        }

        [Fact]
        public void MethodProducesErrorIfNewlineFollowsTransition()
        {
            ParseBlockTest("@" + Environment.NewLine);
        }

        [Fact]
        public void MethodProducesErrorIfWhitespaceBetweenTransitionAndBlockStartInEmbeddedExpr()
        {
            // ParseBlockMethodProducesErrorIfWhitespaceBetweenTransitionAndBlockStartInEmbeddedExpression
            ParseBlockTest("{" + Environment.NewLine
                         + "    @   {}" + Environment.NewLine
                         + "}");
        }

        [Fact]
        public void MethodProducesErrorIfEOFAfterTransitionInEmbeddedExpression()
        {
            ParseBlockTest("{" + Environment.NewLine
                         + "    @");
        }

        [Fact]
        public void MethodParsesNothingIfFirstCharacterIsNotIdentifierStartOrParenOrBrace()
        {
            ParseBlockTest("@!!!");
        }

        [Fact]
        public void ShouldReportErrorAndTerminateAtEOFIfIfParenInExplicitExprUnclosed()
        {
            // ParseBlockShouldReportErrorAndTerminateAtEOFIfIfParenInExplicitExpressionUnclosed
            ParseBlockTest("(foo bar" + Environment.NewLine
                         + "baz");
        }

        [Fact]
        public void ShouldReportErrorAndTerminateAtMarkupIfIfParenInExplicitExprUnclosed()
        {
            // ParseBlockShouldReportErrorAndTerminateAtMarkupIfIfParenInExplicitExpressionUnclosed
            ParseBlockTest("(foo bar" + Environment.NewLine
                         + "<html>" + Environment.NewLine
                         + "baz" + Environment.NewLine
                         + "</html");
        }

        [Fact]
        public void CorrectlyHandlesInCorrectTransitionsIfImplicitExpressionParensUnclosed()
        {
            ParseBlockTest("Href(" + Environment.NewLine
                         + "<h1>@Html.Foo(Bar);</h1>" + Environment.NewLine);
        }

        [Fact]
        // Test for fix to Dev10 884975 - Incorrect Error Messaging
        public void ShouldReportErrorAndTerminateAtEOFIfParenInImplicitExprUnclosed()
        {
            // ParseBlockShouldReportErrorAndTerminateAtEOFIfParenInImplicitExpressionUnclosed
            ParseBlockTest("Foo(Bar(Baz)" + Environment.NewLine
                            + "Biz" + Environment.NewLine
                            + "Boz");
        }

        [Fact]
        // Test for fix to Dev10 884975 - Incorrect Error Messaging
        public void ShouldReportErrorAndTerminateAtMarkupIfParenInImplicitExpressionUnclosed()
        {
            // ParseBlockShouldReportErrorAndTerminateAtMarkupIfParenInImplicitExpressionUnclosed
            ParseBlockTest("Foo(Bar(Baz)" + Environment.NewLine
                            + "Biz" + Environment.NewLine
                            + "<html>" + Environment.NewLine
                            + "Boz" + Environment.NewLine
                            + "</html>");
        }

        [Fact]
        // Test for fix to Dev10 884975 - Incorrect Error Messaging
        public void ShouldReportErrorAndTerminateAtEOFIfBracketInImplicitExpressionUnclosed()
        {
            // ParseBlockShouldReportErrorAndTerminateAtEOFIfBracketInImplicitExpressionUnclosed
            ParseBlockTest("Foo[Bar[Baz]" + Environment.NewLine
                         + "Biz" + Environment.NewLine
                         + "Boz");
        }

        [Fact]
        // Test for fix to Dev10 884975 - Incorrect Error Messaging
        public void ShouldReportErrorAndTerminateAtMarkupIfBracketInImplicitExprUnclosed()
        {
            // ParseBlockShouldReportErrorAndTerminateAtMarkupIfBracketInImplicitExpressionUnclosed
            ParseBlockTest("Foo[Bar[Baz]" + Environment.NewLine
                         + "Biz" + Environment.NewLine
                         + "<b>" + Environment.NewLine
                         + "Boz" + Environment.NewLine
                         + "</b>");
        }

        // Simple EOF handling errors:
        [Fact]
        public void ReportsErrorIfExplicitCodeBlockUnterminatedAtEOF()
        {
            ParseBlockTest("{ var foo = bar; if(foo != null) { bar(); } ");
        }

        [Fact]
        public void ReportsErrorIfClassBlockUnterminatedAtEOF()
        {
            ParseBlockTest(
                "functions { var foo = bar; if(foo != null) { bar(); } ",
                new[] { FunctionsDirective.Directive });
        }

        [Fact]
        public void ReportsErrorIfIfBlockUnterminatedAtEOF()
        {
            RunUnterminatedSimpleKeywordBlock("if");
        }

        [Fact]
        public void ReportsErrorIfElseBlockUnterminatedAtEOF()
        {
            ParseBlockTest("if(foo) { baz(); } else { var foo = bar; if(foo != null) { bar(); } ");
        }

        [Fact]
        public void ReportsErrorIfElseIfBlockUnterminatedAtEOF()
        {
            ParseBlockTest("if(foo) { baz(); } else if { var foo = bar; if(foo != null) { bar(); } ");
        }

        [Fact]
        public void ReportsErrorIfDoBlockUnterminatedAtEOF()
        {
            ParseBlockTest("do { var foo = bar; if(foo != null) { bar(); } ");
        }

        [Fact]
        public void ReportsErrorIfTryBlockUnterminatedAtEOF()
        {
            ParseBlockTest("try { var foo = bar; if(foo != null) { bar(); } ");
        }

        [Fact]
        public void ReportsErrorIfCatchBlockUnterminatedAtEOF()
        {
            ParseBlockTest("try { baz(); } catch(Foo) { var foo = bar; if(foo != null) { bar(); } ");
        }

        [Fact]
        public void ReportsErrorIfFinallyBlockUnterminatedAtEOF()
        {
            ParseBlockTest("try { baz(); } finally { var foo = bar; if(foo != null) { bar(); } ");
        }

        [Fact]
        public void ReportsErrorIfForBlockUnterminatedAtEOF()
        {
            RunUnterminatedSimpleKeywordBlock("for");
        }

        [Fact]
        public void ReportsErrorIfForeachBlockUnterminatedAtEOF()
        {
            RunUnterminatedSimpleKeywordBlock("foreach");
        }

        [Fact]
        public void ReportsErrorIfWhileBlockUnterminatedAtEOF()
        {
            RunUnterminatedSimpleKeywordBlock("while");
        }

        [Fact]
        public void ReportsErrorIfSwitchBlockUnterminatedAtEOF()
        {
            RunUnterminatedSimpleKeywordBlock("switch");
        }

        [Fact]
        public void ReportsErrorIfLockBlockUnterminatedAtEOF()
        {
            RunUnterminatedSimpleKeywordBlock("lock");
        }

        [Fact]
        public void ReportsErrorIfUsingBlockUnterminatedAtEOF()
        {
            RunUnterminatedSimpleKeywordBlock("using");
        }

        [Fact]
        public void RequiresControlFlowStatementsToHaveBraces()
        {
            ParseBlockTest("if(foo) <p>Bar</p> else if(bar) <p>Baz</p> else <p>Boz</p>");
        }

        [Fact]
        public void IncludesUnexpectedCharacterInSingleStatementControlFlowStatementError()
        {
            ParseBlockTest("if(foo)) { var bar = foo; }");
        }

        [Fact]
        public void OutputsErrorIfAtSignFollowedByLessThanSignAtStatementStart()
        {
            ParseBlockTest("if(foo) { @<p>Bar</p> }");
        }

        [Fact]
        public void TerminatesIfBlockAtEOLWhenRecoveringFromMissingCloseParen()
        {
            ParseBlockTest("if(foo bar" + Environment.NewLine
                         + "baz");
        }

        [Fact]
        public void TerminatesForeachBlockAtEOLWhenRecoveringFromMissingCloseParen()
        {
            ParseBlockTest("foreach(foo bar" + Environment.NewLine
                         + "baz");
        }

        [Fact]
        public void TerminatesWhileClauseInDoStmtAtEOLWhenRecoveringFromMissingCloseParen()
        {
            ParseBlockTest("do { } while(foo bar" + Environment.NewLine
                         + "baz");
        }

        [Fact]
        public void TerminatesUsingBlockAtEOLWhenRecoveringFromMissingCloseParen()
        {
            ParseBlockTest("using(foo bar" + Environment.NewLine
                         + "baz");
        }

        [Fact]
        public void ResumesIfStatementAfterOpenParen()
        {
            ParseBlockTest("if(" + Environment.NewLine
                         + "else { <p>Foo</p> }");
        }

        [Fact]
        public void TerminatesNormalCSharpStringsAtEOLIfEndQuoteMissing()
        {
            SingleSpanBlockTest("if(foo) {" + Environment.NewLine
                              + "    var p = \"foo bar baz" + Environment.NewLine
                              + ";" + Environment.NewLine
                              + "}");
        }

        [Fact]
        public void TerminatesNormalStringAtEndOfFile()
        {
            SingleSpanBlockTest("if(foo) { var foo = \"blah blah blah blah blah");
        }

        [Fact]
        public void TerminatesVerbatimStringAtEndOfFile()
        {
            SingleSpanBlockTest("if(foo) { var foo = @\"blah " + Environment.NewLine
                              + "blah; " + Environment.NewLine
                              + "<p>Foo</p>" + Environment.NewLine
                              + "blah " + Environment.NewLine
                              + "blah");
        }

        [Fact]
        public void CorrectlyParsesMarkupIncorrectyAssumedToBeWithinAStatement()
        {
            ParseBlockTest("if(foo) {" + Environment.NewLine
                         + "    var foo = \"foo bar baz" + Environment.NewLine
                         + "    <p>Foo is @foo</p>" + Environment.NewLine
                         + "}");
        }

        [Fact]
        public void CorrectlyParsesAtSignInDelimitedBlock()
        {
            ParseBlockTest("(Request[\"description\"] ?? @photo.Description)");
        }

        [Fact]
        public void CorrectlyRecoversFromMissingCloseParenInExpressionWithinCode()
        {
            ParseBlockTest(@"{string.Format(<html></html>}");
        }

        private void RunUnterminatedSimpleKeywordBlock(string keyword)
        {
            SingleSpanBlockTest(
                keyword + " (foo) { var foo = bar; if(foo != null) { bar(); } ");
        }
    }
}
