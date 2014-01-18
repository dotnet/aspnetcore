// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Razor.Parser;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.Resources;
using Microsoft.AspNet.Razor.Test.Framework;
using Microsoft.AspNet.Razor.Text;
using Microsoft.TestCommon;

namespace Microsoft.AspNet.Razor.Test.Parser.VB
{
    public class VBBlockTest : VBHtmlCodeParserTestBase
    {
        [Fact]
        public void ParseBlockMethodThrowsArgNullExceptionOnNullContext()
        {
            // Arrange
            VBCodeParser parser = new VBCodeParser();

            // Act and Assert
            Assert.Throws<InvalidOperationException>(() => parser.ParseBlock(), RazorResources.Parser_Context_Not_Set);
        }

        [Fact]
        public void ParseBlockAcceptsImplicitExpression()
        {
            ParseBlockTest("If True Then" + Environment.NewLine
                         + "    @foo" + Environment.NewLine
                         + "End If",
                new StatementBlock(
                    Factory.Code("If True Then\r\n    ").AsStatement(),
                    new ExpressionBlock(
                        Factory.CodeTransition(),
                        Factory.Code("foo")
                               .AsImplicitExpression(VBCodeParser.DefaultKeywords, acceptTrailingDot: true)
                               .Accepts(AcceptedCharacters.NonWhiteSpace)),
                    Factory.Code("\r\nEnd If")
                           .AsStatement()
                           .Accepts(AcceptedCharacters.None)));
        }

        [Fact]
        public void ParseBlockAcceptsIfStatementWithinCodeBlockIfInDesignTimeMode()
        {
            ParseBlockTest("If True Then" + Environment.NewLine
                         + "    @If True Then" + Environment.NewLine
                         + "    End If" + Environment.NewLine
                         + "End If",
                new StatementBlock(
                    Factory.Code("If True Then\r\n    ").AsStatement(),
                    new StatementBlock(
                        Factory.CodeTransition(),
                        Factory.Code("If True Then\r\n    End If\r\n")
                               .AsStatement()
                               .Accepts(AcceptedCharacters.None)),
                    Factory.Code(@"End If")
                           .AsStatement()
                           .Accepts(AcceptedCharacters.None)));
        }

        [Fact]
        public void ParseBlockSupportsSpacesInStrings()
        {
            ParseBlockTest("for each p in db.Query(\"SELECT * FROM PRODUCTS\")" + Environment.NewLine
                         + "    @<p>@p.Name</p>" + Environment.NewLine
                         + "next",
                new StatementBlock(
                    Factory.Code("for each p in db.Query(\"SELECT * FROM PRODUCTS\")\r\n")
                           .AsStatement(),
                    new MarkupBlock(
                        Factory.Markup("    "),
                        Factory.MarkupTransition(),
                        Factory.Markup("<p>"),
                        new ExpressionBlock(
                            Factory.CodeTransition(),
                            Factory.Code("p.Name")
                                   .AsImplicitExpression(VBCodeParser.DefaultKeywords)
                                   .Accepts(AcceptedCharacters.NonWhiteSpace)),
                        Factory.Markup("</p>\r\n").Accepts(AcceptedCharacters.None)),
                    Factory.Code("next")
                           .AsStatement()
                           .Accepts(AcceptedCharacters.WhiteSpace | AcceptedCharacters.NonWhiteSpace)));
        }

        [Fact]
        public void ParseBlockSupportsSimpleCodeBlock()
        {
            ParseBlockTest("Code" + Environment.NewLine
                         + "    If foo IsNot Nothing" + Environment.NewLine
                         + "        Bar(foo)" + Environment.NewLine
                         + "    End If" + Environment.NewLine
                         + "End Code",
                new StatementBlock(
                    Factory.MetaCode("Code").Accepts(AcceptedCharacters.None),
                    Factory.Code("\r\n    If foo IsNot Nothing\r\n        Bar(foo)\r\n    End If\r\n")
                           .AsStatement(),
                    Factory.MetaCode("End Code").Accepts(AcceptedCharacters.None)));
        }

        [Fact]
        public void ParseBlockRejectsNewlineBetweenEndAndCodeIfNotPrefixedWithUnderscore()
        {
            ParseBlockTest("Code" + Environment.NewLine
                         + "    If foo IsNot Nothing" + Environment.NewLine
                         + "        Bar(foo)" + Environment.NewLine
                         + "    End If" + Environment.NewLine
                         + "End" + Environment.NewLine
                         + "Code",
                new StatementBlock(
                    Factory.MetaCode("Code").Accepts(AcceptedCharacters.None),
                    Factory.Code("\r\n    If foo IsNot Nothing\r\n        Bar(foo)\r\n    End If\r\nEnd\r\nCode")
                           .AsStatement()),
                new RazorError(
                    String.Format(RazorResources.ParseError_BlockNotTerminated, "Code", "End Code"),
                    SourceLocation.Zero));
        }

        [Fact]
        public void ParseBlockAcceptsNewlineBetweenEndAndCodeIfPrefixedWithUnderscore()
        {
            ParseBlockTest("Code" + Environment.NewLine
                         + "    If foo IsNot Nothing" + Environment.NewLine
                         + "        Bar(foo)" + Environment.NewLine
                         + "    End If" + Environment.NewLine
                         + "End _" + Environment.NewLine
                         + "_" + Environment.NewLine
                         + " _" + Environment.NewLine
                         + "Code",
                new StatementBlock(
                    Factory.MetaCode("Code").Accepts(AcceptedCharacters.None),
                    Factory.Code("\r\n    If foo IsNot Nothing\r\n        Bar(foo)\r\n    End If\r\n")
                           .AsStatement(),
                    Factory.MetaCode("End _\r\n_\r\n _\r\nCode").Accepts(AcceptedCharacters.None)));
        }

        [Fact]
        public void ParseBlockSupportsSimpleFunctionsBlock()
        {
            ParseBlockTest("Functions" + Environment.NewLine
                         + "    Public Sub Foo()" + Environment.NewLine
                         + "        Bar()" + Environment.NewLine
                         + "    End Sub" + Environment.NewLine
                         + Environment.NewLine
                         + "    Private Function Bar() As Object" + Environment.NewLine
                         + "        Return Nothing" + Environment.NewLine
                         + "    End Function" + Environment.NewLine
                         + "End Functions",
                new FunctionsBlock(
                    Factory.MetaCode("Functions").Accepts(AcceptedCharacters.None),
                    Factory.Code("\r\n    Public Sub Foo()\r\n        Bar()\r\n    End Sub\r\n\r\n    Private Function Bar() As Object\r\n        Return Nothing\r\n    End Function\r\n")
                           .AsFunctionsBody(),
                    Factory.MetaCode("End Functions").Accepts(AcceptedCharacters.None)));
        }

        [Fact]
        public void ParseBlockRejectsNewlineBetweenEndAndFunctionsIfNotPrefixedWithUnderscore()
        {
            ParseBlockTest("Functions" + Environment.NewLine
                         + "    If foo IsNot Nothing" + Environment.NewLine
                         + "        Bar(foo)" + Environment.NewLine
                         + "    End If" + Environment.NewLine
                         + "End" + Environment.NewLine
                         + "Functions",
                new FunctionsBlock(
                    Factory.MetaCode("Functions").Accepts(AcceptedCharacters.None),
                    Factory.Code("\r\n    If foo IsNot Nothing\r\n        Bar(foo)\r\n    End If\r\nEnd\r\nFunctions")
                           .AsFunctionsBody()),
                new RazorError(
                    String.Format(RazorResources.ParseError_BlockNotTerminated, "Functions", "End Functions"),
                    SourceLocation.Zero));
        }

        [Fact]
        public void ParseBlockAcceptsNewlineBetweenEndAndFunctionsIfPrefixedWithUnderscore()
        {
            ParseBlockTest("Functions" + Environment.NewLine
                         + "    If foo IsNot Nothing" + Environment.NewLine
                         + "        Bar(foo)" + Environment.NewLine
                         + "    End If" + Environment.NewLine
                         + "End _" + Environment.NewLine
                         + "_" + Environment.NewLine
                         + " _" + Environment.NewLine
                         + "Functions",
                new FunctionsBlock(
                    Factory.MetaCode("Functions").Accepts(AcceptedCharacters.None),
                    Factory.Code("\r\n    If foo IsNot Nothing\r\n        Bar(foo)\r\n    End If\r\n")
                           .AsFunctionsBody(),
                    Factory.MetaCode("End _\r\n_\r\n _\r\nFunctions").Accepts(AcceptedCharacters.None)));
        }

        [Fact]
        public void ParseBlockCorrectlyHandlesExtraEndsInEndCode()
        {
            ParseBlockTest("Code" + Environment.NewLine
                         + "    Bar End" + Environment.NewLine
                         + "End Code",
                new StatementBlock(
                    Factory.MetaCode("Code").Accepts(AcceptedCharacters.None),
                    Factory.Code("\r\n    Bar End\r\n").AsStatement(),
                    Factory.MetaCode("End Code").Accepts(AcceptedCharacters.None)));
        }

        [Fact]
        public void ParseBlockCorrectlyHandlesExtraEndsInEndFunctions()
        {
            ParseBlockTest("Functions" + Environment.NewLine
                         + "    Bar End" + Environment.NewLine
                         + "End Functions",
                new FunctionsBlock(
                    Factory.MetaCode("Functions").Accepts(AcceptedCharacters.None),
                    Factory.Code("\r\n    Bar End\r\n").AsFunctionsBody().AutoCompleteWith(null, atEndOfSpan: false),
                    Factory.MetaCode("End Functions").Accepts(AcceptedCharacters.None)));
        }

        [Theory]
        [InlineData("If", "End", "If")]
        [InlineData("Try", "End", "Try")]
        [InlineData("While", "End", "While")]
        [InlineData("Using", "End", "Using")]
        [InlineData("With", "End", "With")]
        public void KeywordAllowsNewlinesIfPrefixedByUnderscore(string startKeyword, string endKeyword1, string endKeyword2)
        {
            string code = startKeyword + Environment.NewLine
                        + "    ' In the block" + Environment.NewLine
                        + endKeyword1 + " _" + Environment.NewLine
                        + "_" + Environment.NewLine
                        + "_" + Environment.NewLine
                        + "_" + Environment.NewLine
                        + "_" + Environment.NewLine
                        + "_" + Environment.NewLine
                        + "  " + endKeyword2 + Environment.NewLine;
            ParseBlockTest(code + "foo bar baz",
                new StatementBlock(
                    Factory.Code(code)
                           .AsStatement()
                           .Accepts(AcceptedCharacters.None)));
        }

        [Theory]
        [InlineData("While", "EndWhile", "End While")]
        [InlineData("If", "EndIf", "End If")]
        [InlineData("Select", "EndSelect", "End Select")]
        [InlineData("Try", "EndTry", "End Try")]
        [InlineData("With", "EndWith", "End With")]
        [InlineData("Using", "EndUsing", "End Using")]
        public void EndTerminatedKeywordRequiresSpaceBetweenEndAndKeyword(string startKeyword, string wrongEndKeyword, string endKeyword)
        {
            string code = startKeyword + Environment.NewLine
                        + "    ' This should not end the code" + Environment.NewLine
                        + "    " + wrongEndKeyword + Environment.NewLine
                        + "    ' But this should" + Environment.NewLine
                        + endKeyword;
            ParseBlockTest(code,
                new StatementBlock(
                    Factory.Code(code)
                           .AsStatement()
                           .Accepts(AcceptedCharacters.None)));
        }

        [Theory]
        [InlineData("While", "End While", false)]
        [InlineData("Do", "Loop", true)]
        [InlineData("If", "End If", false)]
        [InlineData("Select", "End Select", false)]
        [InlineData("For", "Next", true)]
        [InlineData("Try", "End Try", false)]
        [InlineData("With", "End With", false)]
        [InlineData("Using", "End Using", false)]
        public void EndSequenceInString(string keyword, string endSequence, bool acceptToEndOfLine)
        {
            string code = keyword + Environment.NewLine
                        + "    \"" + endSequence + "\"" + Environment.NewLine
                        + endSequence + (acceptToEndOfLine ? " foo bar baz" : "") + Environment.NewLine;
            ParseBlockTest(code + "biz boz",
                new StatementBlock(
                    Factory.Code(code).AsStatement().Accepts(GetAcceptedCharacters(acceptToEndOfLine))));
        }

        [Theory]
        [InlineData("While", "End While", false)]
        [InlineData("Do", "Loop", true)]
        [InlineData("If", "End If", false)]
        [InlineData("Select", "End Select", false)]
        [InlineData("For", "Next", true)]
        [InlineData("Try", "End Try", false)]
        [InlineData("With", "End With", false)]
        [InlineData("Using", "End Using", false)]
        private void CommentedEndSequence(string keyword, string endSequence, bool acceptToEndOfLine)
        {
            string code = keyword + Environment.NewLine
                        + "    '" + endSequence + Environment.NewLine
                        + endSequence + (acceptToEndOfLine ? @" foo bar baz" : "") + Environment.NewLine;
            ParseBlockTest(code + "biz boz",
                new StatementBlock(
                    Factory.Code(code).AsStatement().Accepts(GetAcceptedCharacters(acceptToEndOfLine))));
        }

        [Theory]
        [InlineData("While", "End While", false)]
        [InlineData("Do", "Loop", true)]
        [InlineData("If", "End If", false)]
        [InlineData("Select", "End Select", false)]
        [InlineData("For", "Next", true)]
        [InlineData("Try", "End Try", false)]
        [InlineData("With", "End With", false)]
        [InlineData("SyncLock", "End SyncLock", false)]
        [InlineData("Using", "End Using", false)]
        private void NestedKeywordBlock(string keyword, string endSequence, bool acceptToEndOfLine)
        {
            string code = keyword + Environment.NewLine
                        + "    " + keyword + Environment.NewLine
                        + "        Bar(foo)" + Environment.NewLine
                        + "    " + endSequence + Environment.NewLine
                        + endSequence + (acceptToEndOfLine ? " foo bar baz" : "") + Environment.NewLine;
            ParseBlockTest(code + "biz boz",
                new StatementBlock(
                    Factory.Code(code).AsStatement().Accepts(GetAcceptedCharacters(acceptToEndOfLine))));
        }

        [Theory]
        [InlineData("While True", "End While", false)]
        [InlineData("Do", "Loop", true)]
        [InlineData("If foo IsNot Nothing", "End If", false)]
        [InlineData("Select Case foo", "End Select", false)]
        [InlineData("For Each p in Products", "Next", true)]
        [InlineData("Try", "End Try", false)]
        [InlineData("With", "End With", false)]
        [InlineData("SyncLock", "End SyncLock", false)]
        [InlineData("Using", "End Using", false)]
        private void SimpleKeywordBlock(string keyword, string endSequence, bool acceptToEndOfLine)
        {
            string code = keyword + Environment.NewLine
                        + "    Bar(foo)" + Environment.NewLine
                        + endSequence + (acceptToEndOfLine ? " foo bar baz" : "") + Environment.NewLine;
            ParseBlockTest(code + "biz boz",
                new StatementBlock(
                    Factory.Code(code).AsStatement().Accepts(GetAcceptedCharacters(acceptToEndOfLine))));
        }

        [Theory]
        [InlineData("While True", "Exit While", "End While", false)]
        [InlineData("Do", "Exit Do", "Loop", true)]
        [InlineData("For Each p in Products", "Exit For", "Next", true)]
        [InlineData("While True", "Continue While", "End While", false)]
        [InlineData("Do", "Continue Do", "Loop", true)]
        [InlineData("For Each p in Products", "Continue For", "Next", true)]
        private void KeywordWithExitOrContinue(string startKeyword, string exitKeyword, string endKeyword, bool acceptToEndOfLine)
        {
            string code = startKeyword + Environment.NewLine
                         + "    ' This is before the exit" + Environment.NewLine
                         + "    " + exitKeyword + Environment.NewLine
                         + "    ' This is after the exit" + Environment.NewLine
                         + endKeyword + Environment.NewLine;
            ParseBlockTest(code + "foo bar baz",
                new StatementBlock(
                    Factory.Code(code).AsStatement().Accepts(GetAcceptedCharacters(acceptToEndOfLine))));
        }

        private AcceptedCharacters GetAcceptedCharacters(bool acceptToEndOfLine)
        {
            return acceptToEndOfLine ?
                AcceptedCharacters.WhiteSpace | AcceptedCharacters.NonWhiteSpace :
                AcceptedCharacters.None;
        }
    }
}
