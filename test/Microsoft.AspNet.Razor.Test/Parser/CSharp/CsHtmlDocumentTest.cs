// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Razor.Parser;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.Test.Framework;
using Microsoft.AspNet.Razor.Text;
using Microsoft.AspNet.Razor.Tokenizer.Symbols;
using Microsoft.TestCommon;

namespace Microsoft.AspNet.Razor.Test.Parser.CSharp
{
    public class CsHtmlDocumentTest : CsHtmlMarkupParserTestBase
    {
        [Fact]
        public void UnterminatedBlockCommentCausesRazorError()
        {
            ParseDocumentTest("@* Foo Bar",
                              new MarkupBlock(
                                  Factory.EmptyHtml(),
                                  new CommentBlock(
                                      Factory.MarkupTransition(HtmlSymbolType.RazorCommentTransition),
                                      Factory.MetaMarkup("*", HtmlSymbolType.RazorCommentStar).Accepts(AcceptedCharacters.None),
                                      Factory.Comment(" Foo Bar", HtmlSymbolType.RazorComment)
                                      )
                                  ),
                              new RazorError(RazorResources.ParseError_RazorComment_Not_Terminated, SourceLocation.Zero));
        }

        [Fact]
        public void BlockCommentInMarkupDocumentIsHandledCorrectly()
        {
            ParseDocumentTest("<ul>" + Environment.NewLine
                            + "                @* This is a block comment </ul> *@ foo",
                              new MarkupBlock(
                                  Factory.Markup("<ul>\r\n                "),
                                  new CommentBlock(
                                      Factory.MarkupTransition(HtmlSymbolType.RazorCommentTransition),
                                      Factory.MetaMarkup("*", HtmlSymbolType.RazorCommentStar).Accepts(AcceptedCharacters.None),
                                      Factory.Comment(" This is a block comment </ul> ", HtmlSymbolType.RazorComment),
                                      Factory.MetaMarkup("*", HtmlSymbolType.RazorCommentStar).Accepts(AcceptedCharacters.None),
                                      Factory.MarkupTransition(HtmlSymbolType.RazorCommentTransition)
                                      ),
                                  Factory.Markup(" foo")
                                  ));
        }

        [Fact]
        public void BlockCommentInMarkupBlockIsHandledCorrectly()
        {
            ParseBlockTest("<ul>" + Environment.NewLine
                         + "                @* This is a block comment </ul> *@ foo </ul>",
                           new MarkupBlock(
                               Factory.Markup("<ul>\r\n                "),
                               new CommentBlock(
                                   Factory.MarkupTransition(HtmlSymbolType.RazorCommentTransition),
                                   Factory.MetaMarkup("*", HtmlSymbolType.RazorCommentStar).Accepts(AcceptedCharacters.None),
                                   Factory.Comment(" This is a block comment </ul> ", HtmlSymbolType.RazorComment),
                                   Factory.MetaMarkup("*", HtmlSymbolType.RazorCommentStar).Accepts(AcceptedCharacters.None),
                                   Factory.MarkupTransition(HtmlSymbolType.RazorCommentTransition)
                                   ),
                               Factory.Markup(" foo </ul>").Accepts(AcceptedCharacters.None)
                               ));
        }

        [Fact]
        public void BlockCommentAtStatementStartInCodeBlockIsHandledCorrectly()
        {
            ParseDocumentTest("@if(Request.IsAuthenticated) {" + Environment.NewLine
                            + "    @* User is logged in! } *@" + Environment.NewLine
                            + "    Write(\"Hello friend!\");" + Environment.NewLine
                            + "}",
                              new MarkupBlock(
                                  Factory.EmptyHtml(),
                                  new StatementBlock(
                                      Factory.CodeTransition(),
                                      Factory.Code("if(Request.IsAuthenticated) {\r\n    ").AsStatement(),
                                      new CommentBlock(
                                          Factory.CodeTransition(CSharpSymbolType.RazorCommentTransition),
                                          Factory.MetaCode("*", CSharpSymbolType.RazorCommentStar).Accepts(AcceptedCharacters.None),
                                          Factory.Comment(" User is logged in! } ", CSharpSymbolType.RazorComment),
                                          Factory.MetaCode("*", CSharpSymbolType.RazorCommentStar).Accepts(AcceptedCharacters.None),
                                          Factory.CodeTransition(CSharpSymbolType.RazorCommentTransition)
                                          ),
                                      Factory.Code("\r\n    Write(\"Hello friend!\");\r\n}").AsStatement())));
        }

        [Fact]
        public void BlockCommentInStatementInCodeBlockIsHandledCorrectly()
        {
            ParseDocumentTest("@if(Request.IsAuthenticated) {" + Environment.NewLine
                            + "    var foo = @* User is logged in! ; *@;" + Environment.NewLine
                            + "    Write(\"Hello friend!\");" + Environment.NewLine
                            + "}",
                              new MarkupBlock(
                                  Factory.EmptyHtml(),
                                  new StatementBlock(
                                      Factory.CodeTransition(),
                                      Factory.Code("if(Request.IsAuthenticated) {\r\n    var foo = ").AsStatement(),
                                      new CommentBlock(
                                          Factory.CodeTransition(CSharpSymbolType.RazorCommentTransition),
                                          Factory.MetaCode("*", CSharpSymbolType.RazorCommentStar).Accepts(AcceptedCharacters.None),
                                          Factory.Comment(" User is logged in! ; ", CSharpSymbolType.RazorComment),
                                          Factory.MetaCode("*", CSharpSymbolType.RazorCommentStar).Accepts(AcceptedCharacters.None),
                                          Factory.CodeTransition(CSharpSymbolType.RazorCommentTransition)
                                          ),
                                      Factory.Code(";\r\n    Write(\"Hello friend!\");\r\n}").AsStatement())));
        }

        [Fact]
        public void BlockCommentInStringIsIgnored()
        {
            ParseDocumentTest("@if(Request.IsAuthenticated) {" + Environment.NewLine
                            + "    var foo = \"@* User is logged in! ; *\";" + Environment.NewLine
                            + "    Write(\"Hello friend!\");" + Environment.NewLine
                            + "}",
                              new MarkupBlock(
                                  Factory.EmptyHtml(),
                                  new StatementBlock(
                                      Factory.CodeTransition(),
                                      Factory.Code("if(Request.IsAuthenticated) {" + Environment.NewLine
                                                 + "    var foo = \"@* User is logged in! ; *\";" + Environment.NewLine
                                                 + "    Write(\"Hello friend!\");" + Environment.NewLine
                                                 + "}").AsStatement())));
        }

        [Fact]
        public void BlockCommentInCSharpBlockCommentIsIgnored()
        {
            ParseDocumentTest("@if(Request.IsAuthenticated) {" + Environment.NewLine
                            + "    var foo = /*@* User is logged in! */ *@ */;" + Environment.NewLine
                            + "    Write(\"Hello friend!\");" + Environment.NewLine
                            + "}",
                              new MarkupBlock(
                                  Factory.EmptyHtml(),
                                  new StatementBlock(
                                      Factory.CodeTransition(),
                                      Factory.Code("if(Request.IsAuthenticated) {" + Environment.NewLine
                                                 + "    var foo = /*@* User is logged in! */ *@ */;" + Environment.NewLine
                                                 + "    Write(\"Hello friend!\");" + Environment.NewLine
                                                 + "}").AsStatement())));
        }

        [Fact]
        public void BlockCommentInCSharpLineCommentIsIgnored()
        {
            ParseDocumentTest("@if(Request.IsAuthenticated) {" + Environment.NewLine
                            + "    var foo = //@* User is logged in! */ *@;" + Environment.NewLine
                            + "    Write(\"Hello friend!\");" + Environment.NewLine
                            + "}",
                              new MarkupBlock(
                                  Factory.EmptyHtml(),
                                  new StatementBlock(
                                      Factory.CodeTransition(),
                                      Factory.Code("if(Request.IsAuthenticated) {" + Environment.NewLine
                                                 + "    var foo = //@* User is logged in! */ *@;" + Environment.NewLine
                                                 + "    Write(\"Hello friend!\");" + Environment.NewLine
                                                 + "}").AsStatement())));
        }

        [Fact]
        public void BlockCommentInImplicitExpressionIsHandledCorrectly()
        {
            ParseDocumentTest("@Html.Foo@*bar*@",
                              new MarkupBlock(
                                  Factory.EmptyHtml(),
                                  new ExpressionBlock(
                                      Factory.CodeTransition(),
                                      Factory.Code("Html.Foo").AsImplicitExpression(CSharpCodeParser.DefaultKeywords).Accepts(AcceptedCharacters.NonWhiteSpace)
                                      ),
                                  Factory.EmptyHtml(),
                                  new CommentBlock(
                                      Factory.MarkupTransition(HtmlSymbolType.RazorCommentTransition),
                                      Factory.MetaMarkup("*", HtmlSymbolType.RazorCommentStar).Accepts(AcceptedCharacters.None),
                                      Factory.Comment("bar", HtmlSymbolType.RazorComment),
                                      Factory.MetaMarkup("*", HtmlSymbolType.RazorCommentStar).Accepts(AcceptedCharacters.None),
                                      Factory.MarkupTransition(HtmlSymbolType.RazorCommentTransition)
                                      ),
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
                                      Factory.Code("Html").AsImplicitExpression(CSharpCodeParser.DefaultKeywords).Accepts(AcceptedCharacters.NonWhiteSpace)
                                      ),
                                  Factory.Markup("."),
                                  new CommentBlock(
                                      Factory.MarkupTransition(HtmlSymbolType.RazorCommentTransition),
                                      Factory.MetaMarkup("*", HtmlSymbolType.RazorCommentStar).Accepts(AcceptedCharacters.None),
                                      Factory.Comment("bar", HtmlSymbolType.RazorComment),
                                      Factory.MetaMarkup("*", HtmlSymbolType.RazorCommentStar).Accepts(AcceptedCharacters.None),
                                      Factory.MarkupTransition(HtmlSymbolType.RazorCommentTransition)
                                      ),
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
                                      Factory.Code("Html.Foo(").AsImplicitExpression(CSharpCodeParser.DefaultKeywords).Accepts(AcceptedCharacters.Any),
                                      new CommentBlock(
                                          Factory.CodeTransition(CSharpSymbolType.RazorCommentTransition),
                                          Factory.MetaCode("*", CSharpSymbolType.RazorCommentStar).Accepts(AcceptedCharacters.None),
                                          Factory.Comment("bar", CSharpSymbolType.RazorComment),
                                          Factory.MetaCode("*", CSharpSymbolType.RazorCommentStar).Accepts(AcceptedCharacters.None),
                                          Factory.CodeTransition(CSharpSymbolType.RazorCommentTransition)
                                          ),
                                      Factory.Code(" 4)").AsImplicitExpression(CSharpCodeParser.DefaultKeywords).Accepts(AcceptedCharacters.NonWhiteSpace)
                                      ),
                                  Factory.EmptyHtml()));
        }

        [Fact]
        public void BlockCommentInBracketsOfImplicitExpressionIsHandledCorrectly()
        {
            ParseDocumentTest("@Html.Foo[@*bar*@ 4]",
                              new MarkupBlock(
                                  Factory.EmptyHtml(),
                                  new ExpressionBlock(
                                      Factory.CodeTransition(),
                                      Factory.Code("Html.Foo[").AsImplicitExpression(CSharpCodeParser.DefaultKeywords).Accepts(AcceptedCharacters.Any),
                                      new CommentBlock(
                                          Factory.CodeTransition(CSharpSymbolType.RazorCommentTransition),
                                          Factory.MetaCode("*", CSharpSymbolType.RazorCommentStar).Accepts(AcceptedCharacters.None),
                                          Factory.Comment("bar", CSharpSymbolType.RazorComment),
                                          Factory.MetaCode("*", CSharpSymbolType.RazorCommentStar).Accepts(AcceptedCharacters.None),
                                          Factory.CodeTransition(CSharpSymbolType.RazorCommentTransition)
                                          ),
                                      Factory.Code(" 4]").AsImplicitExpression(CSharpCodeParser.DefaultKeywords).Accepts(AcceptedCharacters.NonWhiteSpace)
                                      ),
                                  Factory.EmptyHtml()));
        }

        [Fact]
        public void BlockCommentInParensOfConditionIsHandledCorrectly()
        {
            ParseDocumentTest("@if(@*bar*@) {}",
                              new MarkupBlock(
                                  Factory.EmptyHtml(),
                                  new StatementBlock(
                                      Factory.CodeTransition(),
                                      Factory.Code("if(").AsStatement(),
                                      new CommentBlock(
                                          Factory.CodeTransition(CSharpSymbolType.RazorCommentTransition),
                                          Factory.MetaCode("*", CSharpSymbolType.RazorCommentStar).Accepts(AcceptedCharacters.None),
                                          Factory.Comment("bar", CSharpSymbolType.RazorComment),
                                          Factory.MetaCode("*", CSharpSymbolType.RazorCommentStar).Accepts(AcceptedCharacters.None),
                                          Factory.CodeTransition(CSharpSymbolType.RazorCommentTransition)
                                          ),
                                      Factory.Code(") {}").AsStatement()
                                      )));
        }

        [Fact]
        public void BlockCommentInExplicitExpressionIsHandledCorrectly()
        {
            ParseDocumentTest("@(1 + @*bar*@ 1)",
                              new MarkupBlock(
                                  Factory.EmptyHtml(),
                                  new ExpressionBlock(
                                      Factory.CodeTransition(),
                                      Factory.MetaCode("(").Accepts(AcceptedCharacters.None),
                                      Factory.Code("1 + ").AsExpression(),
                                      new CommentBlock(
                                          Factory.CodeTransition(CSharpSymbolType.RazorCommentTransition),
                                          Factory.MetaCode("*", CSharpSymbolType.RazorCommentStar).Accepts(AcceptedCharacters.None),
                                          Factory.Comment("bar", CSharpSymbolType.RazorComment),
                                          Factory.MetaCode("*", CSharpSymbolType.RazorCommentStar).Accepts(AcceptedCharacters.None),
                                          Factory.CodeTransition(CSharpSymbolType.RazorCommentTransition)
                                          ),
                                      Factory.Code(" 1").AsExpression(),
                                      Factory.MetaCode(")").Accepts(AcceptedCharacters.None)
                                      ),
                                  Factory.EmptyHtml()));
        }
    }
}
