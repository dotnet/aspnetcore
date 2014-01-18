// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.Test.Framework;
using Microsoft.AspNet.Razor.Tokenizer.Symbols;
using Microsoft.TestCommon;

namespace Microsoft.AspNet.Razor.Test.Parser.VB
{
    public class VBHtmlDocumentTest : VBHtmlMarkupParserTestBase
    {
        [Fact]
        public void BlockCommentInMarkupDocumentIsHandledCorrectly()
        {
            ParseDocumentTest(@"<ul>" + Environment.NewLine
                            + @"                @* This is a block comment </ul> *@ foo",
                new MarkupBlock(
                    Factory.Markup("<ul>" + Environment.NewLine + "                "),
                    new CommentBlock(
                        Factory.MarkupTransition(HtmlSymbolType.RazorCommentTransition),
                        Factory.MetaMarkup("*", HtmlSymbolType.RazorCommentStar).Accepts(AcceptedCharacters.None),
                        Factory.Comment(" This is a block comment </ul> ", HtmlSymbolType.RazorComment),
                        Factory.MetaMarkup("*", HtmlSymbolType.RazorCommentStar).Accepts(AcceptedCharacters.None),
                        Factory.MarkupTransition(HtmlSymbolType.RazorCommentTransition)),
                    Factory.Markup(" foo")));
        }

        [Fact]
        public void BlockCommentInMarkupBlockIsHandledCorrectly()
        {
            ParseBlockTest(@"<ul>" + Environment.NewLine
                         + @"                @* This is a block comment </ul> *@ foo </ul>",
                new MarkupBlock(
                    Factory.Markup("<ul>" + Environment.NewLine + "                "),
                    new CommentBlock(
                        Factory.MarkupTransition(HtmlSymbolType.RazorCommentTransition),
                        Factory.MetaMarkup("*", HtmlSymbolType.RazorCommentStar).Accepts(AcceptedCharacters.None),
                        Factory.Comment(" This is a block comment </ul> ", HtmlSymbolType.RazorComment),
                        Factory.MetaMarkup("*", HtmlSymbolType.RazorCommentStar).Accepts(AcceptedCharacters.None),
                        Factory.MarkupTransition(HtmlSymbolType.RazorCommentTransition)),
                    Factory.Markup(" foo </ul>").Accepts(AcceptedCharacters.None)));
        }

        [Fact]
        public void BlockCommentAtStatementStartInCodeBlockIsHandledCorrectly()
        {
            ParseDocumentTest(@"@If Request.IsAuthenticated Then" + Environment.NewLine
                            + @"    @* User is logged in! End If *@" + Environment.NewLine
                            + @"    Write(""Hello friend!"")" + Environment.NewLine
                            + @"End If",
                new MarkupBlock(
                    Factory.EmptyHtml(),
                    new StatementBlock(
                        Factory.CodeTransition(),
                        Factory.Code("If Request.IsAuthenticated Then" + Environment.NewLine + "    ").AsStatement(),
                        new CommentBlock(
                            Factory.CodeTransition(VBSymbolType.RazorCommentTransition),
                            Factory.MetaCode("*", VBSymbolType.RazorCommentStar).Accepts(AcceptedCharacters.None),
                            Factory.Comment(" User is logged in! End If ", VBSymbolType.RazorComment),
                            Factory.MetaCode("*", VBSymbolType.RazorCommentStar).Accepts(AcceptedCharacters.None),
                            Factory.CodeTransition(VBSymbolType.RazorCommentTransition)),
                        Factory.Code("" + Environment.NewLine + "    Write(\"Hello friend!\")" + Environment.NewLine + "End If")
                               .AsStatement()
                               .Accepts(AcceptedCharacters.None)),
                    Factory.EmptyHtml()));
        }

        [Fact]
        public void BlockCommentInStatementInCodeBlockIsHandledCorrectly()
        {
            ParseDocumentTest(@"@If Request.IsAuthenticated Then" + Environment.NewLine
                            + @"    Dim foo = @* User is logged in! End If *@ bar" + Environment.NewLine
                            + @"    Write(""Hello friend!"")" + Environment.NewLine
                            + @"End If",
                new MarkupBlock(
                    Factory.EmptyHtml(),
                    new StatementBlock(
                        Factory.CodeTransition(),
                        Factory.Code("If Request.IsAuthenticated Then" + Environment.NewLine + "    Dim foo = ").AsStatement(),
                        new CommentBlock(
                            Factory.CodeTransition(VBSymbolType.RazorCommentTransition),
                            Factory.MetaCode("*", VBSymbolType.RazorCommentStar).Accepts(AcceptedCharacters.None),
                            Factory.Comment(" User is logged in! End If ", VBSymbolType.RazorComment),
                            Factory.MetaCode("*", VBSymbolType.RazorCommentStar).Accepts(AcceptedCharacters.None),
                            Factory.CodeTransition(VBSymbolType.RazorCommentTransition)),
                        Factory.Code(" bar" + Environment.NewLine + "    Write(\"Hello friend!\")" + Environment.NewLine + "End If")
                               .AsStatement()
                               .Accepts(AcceptedCharacters.None)),
                    Factory.EmptyHtml()));
        }

        [Fact]
        public void BlockCommentInStringInCodeBlockIsIgnored()
        {
            ParseDocumentTest(@"@If Request.IsAuthenticated Then" + Environment.NewLine
                            + @"    Dim foo = ""@* User is logged in! End If *@ bar""" + Environment.NewLine
                            + @"    Write(""Hello friend!"")" + Environment.NewLine
                            + @"End If",
                new MarkupBlock(
                    Factory.EmptyHtml(),
                    new StatementBlock(
                        Factory.CodeTransition(),
                        Factory.Code("If Request.IsAuthenticated Then" + Environment.NewLine + "    Dim foo = \"@* User is logged in! End If *@ bar\"" + Environment.NewLine + "    Write(\"Hello friend!\")" + Environment.NewLine + "End If")
                               .AsStatement()
                               .Accepts(AcceptedCharacters.None)),
                    Factory.EmptyHtml()));
        }

        [Fact]
        public void BlockCommentInTickCommentInCodeBlockIsIgnored()
        {
            ParseDocumentTest(@"@If Request.IsAuthenticated Then" + Environment.NewLine
                            + @"    Dim foo = '@* User is logged in! End If *@ bar" + Environment.NewLine
                            + @"    Write(""Hello friend!"")" + Environment.NewLine
                            + @"End If",
                new MarkupBlock(
                    Factory.EmptyHtml(),
                    new StatementBlock(
                        Factory.CodeTransition(),
                        Factory.Code("If Request.IsAuthenticated Then" + Environment.NewLine + "    Dim foo = '@* User is logged in! End If *@ bar" + Environment.NewLine + "    Write(\"Hello friend!\")" + Environment.NewLine + "End If")
                               .AsStatement()
                               .Accepts(AcceptedCharacters.None)),
                    Factory.EmptyHtml()));
        }

        [Fact]
        public void BlockCommentInRemCommentInCodeBlockIsIgnored()
        {
            ParseDocumentTest(@"@If Request.IsAuthenticated Then" + Environment.NewLine
                            + @"    Dim foo = REM @* User is logged in! End If *@ bar" + Environment.NewLine
                            + @"    Write(""Hello friend!"")" + Environment.NewLine
                            + @"End If",
                new MarkupBlock(
                    Factory.EmptyHtml(),
                    new StatementBlock(
                        Factory.CodeTransition(),
                        Factory.Code("If Request.IsAuthenticated Then" + Environment.NewLine + "    Dim foo = REM @* User is logged in! End If *@ bar" + Environment.NewLine + "    Write(\"Hello friend!\")" + Environment.NewLine + "End If")
                               .AsStatement()
                               .Accepts(AcceptedCharacters.None)),
                    Factory.EmptyHtml()));
        }

        [Fact]
        public void BlockCommentInImplicitExpressionIsHandledCorrectly()
        {
            ParseDocumentTest("@Html.Foo@*bar*@",
                new MarkupBlock(
                    Factory.EmptyHtml(),
                    new ExpressionBlock(
                        Factory.CodeTransition(),
                        Factory.Code("Html.Foo")
                               .AsImplicitExpression(KeywordSet)
                               .Accepts(AcceptedCharacters.NonWhiteSpace)),
                    Factory.EmptyHtml(),
                    new CommentBlock(
                        Factory.MarkupTransition(HtmlSymbolType.RazorCommentTransition),
                        Factory.MetaMarkup("*", HtmlSymbolType.RazorCommentStar).Accepts(AcceptedCharacters.None),
                        Factory.Comment("bar", HtmlSymbolType.RazorComment),
                        Factory.MetaMarkup("*", HtmlSymbolType.RazorCommentStar).Accepts(AcceptedCharacters.None),
                        Factory.MarkupTransition(HtmlSymbolType.RazorCommentTransition)),
                    Factory.EmptyHtml()));
        }

        [Fact]
        public void BlockCommentAfterDotOfImplicitExpressionIsHandledCorrectly()
        {
            ParseDocumentTest("@Html.@*bar*@",
                new MarkupBlock(
                    Factory.EmptyHtml(),
                    new ExpressionBlock(
                        Factory.CodeTransition(),
                        Factory.Code("Html")
                               .AsImplicitExpression(KeywordSet)
                               .Accepts(AcceptedCharacters.NonWhiteSpace)),
                    Factory.Markup("."),
                    new CommentBlock(
                        Factory.MarkupTransition(HtmlSymbolType.RazorCommentTransition),
                        Factory.MetaMarkup("*", HtmlSymbolType.RazorCommentStar).Accepts(AcceptedCharacters.None),
                        Factory.Comment("bar", HtmlSymbolType.RazorComment),
                        Factory.MetaMarkup("*", HtmlSymbolType.RazorCommentStar).Accepts(AcceptedCharacters.None),
                        Factory.MarkupTransition(HtmlSymbolType.RazorCommentTransition)),
                    Factory.EmptyHtml()));
        }

        [Fact]
        public void BlockCommentInParensOfImplicitExpressionIsHandledCorrectly()
        {
            ParseDocumentTest("@Html.Foo(@*bar*@ 4)",
                new MarkupBlock(
                    Factory.EmptyHtml(),
                    new ExpressionBlock(
                        Factory.CodeTransition(),
                        Factory.Code("Html.Foo(")
                               .AsImplicitExpression(KeywordSet)
                               .Accepts(AcceptedCharacters.Any),
                        new CommentBlock(
                            Factory.CodeTransition(VBSymbolType.RazorCommentTransition),
                            Factory.MetaCode("*", VBSymbolType.RazorCommentStar).Accepts(AcceptedCharacters.None),
                            Factory.Comment("bar", VBSymbolType.RazorComment),
                            Factory.MetaCode("*", VBSymbolType.RazorCommentStar).Accepts(AcceptedCharacters.None),
                            Factory.CodeTransition(VBSymbolType.RazorCommentTransition)),
                        Factory.Code(" 4)")
                               .AsImplicitExpression(KeywordSet)
                               .Accepts(AcceptedCharacters.NonWhiteSpace)),
                    Factory.EmptyHtml()));
        }

        [Fact]
        public void BlockCommentInConditionIsHandledCorrectly()
        {
            ParseDocumentTest("@If @*bar*@ Then End If",
                new MarkupBlock(
                    Factory.EmptyHtml(),
                    new StatementBlock(
                        Factory.CodeTransition(),
                        Factory.Code("If ").AsStatement(),
                        new CommentBlock(
                            Factory.CodeTransition(VBSymbolType.RazorCommentTransition),
                            Factory.MetaCode("*", VBSymbolType.RazorCommentStar).Accepts(AcceptedCharacters.None),
                            Factory.Comment("bar", VBSymbolType.RazorComment),
                            Factory.MetaCode("*", VBSymbolType.RazorCommentStar).Accepts(AcceptedCharacters.None),
                            Factory.CodeTransition(VBSymbolType.RazorCommentTransition)),
                        Factory.Code(" Then End If").AsStatement().Accepts(AcceptedCharacters.None)),
                    Factory.EmptyHtml()));
        }

        [Fact]
        public void BlockCommentInExplicitExpressionIsHandledCorrectly()
        {
            ParseDocumentTest(@"@(1 + @*bar*@ 1)",
                new MarkupBlock(
                    Factory.EmptyHtml(),
                    new ExpressionBlock(
                        Factory.CodeTransition(),
                        Factory.MetaCode("(").Accepts(AcceptedCharacters.None),
                        Factory.Code(@"1 + ").AsExpression(),
                        new CommentBlock(
                            Factory.CodeTransition(VBSymbolType.RazorCommentTransition),
                            Factory.MetaCode("*", VBSymbolType.RazorCommentStar).Accepts(AcceptedCharacters.None),
                            Factory.Comment("bar", VBSymbolType.RazorComment),
                            Factory.MetaCode("*", VBSymbolType.RazorCommentStar).Accepts(AcceptedCharacters.None),
                            Factory.CodeTransition(VBSymbolType.RazorCommentTransition)
                            ),
                        Factory.Code(" 1").AsExpression(),
                        Factory.MetaCode(")").Accepts(AcceptedCharacters.None)
                        ),
                    Factory.EmptyHtml()));
        }
    }
}
