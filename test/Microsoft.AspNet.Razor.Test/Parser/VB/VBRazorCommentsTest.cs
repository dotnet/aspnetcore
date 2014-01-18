// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Razor.Parser;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.Resources;
using Microsoft.AspNet.Razor.Test.Framework;
using Microsoft.AspNet.Razor.Tokenizer.Symbols;
using Microsoft.TestCommon;

namespace Microsoft.AspNet.Razor.Test.Parser.VB
{
    public class VBRazorCommentsTest : VBHtmlMarkupParserTestBase
    {
        [Fact]
        public void UnterminatedRazorComment()
        {
            ParseDocumentTest("@*",
                new MarkupBlock(
                    Factory.EmptyHtml(),
                    new CommentBlock(
                        Factory.MarkupTransition(HtmlSymbolType.RazorCommentTransition)
                               .Accepts(AcceptedCharacters.None),
                        Factory.MetaMarkup("*", HtmlSymbolType.RazorCommentStar)
                               .Accepts(AcceptedCharacters.None),
                        Factory.Span(SpanKind.Comment, new HtmlSymbol(
                            Factory.LocationTracker.CurrentLocation,
                            String.Empty,
                            HtmlSymbolType.Unknown))
                               .Accepts(AcceptedCharacters.Any))),
                new RazorError(RazorResources.ParseError_RazorComment_Not_Terminated, 0, 0, 0));
        }

        [Fact]
        public void EmptyRazorComment()
        {
            ParseDocumentTest("@**@",
                new MarkupBlock(
                    Factory.EmptyHtml(),
                    new CommentBlock(
                        Factory.MarkupTransition(HtmlSymbolType.RazorCommentTransition)
                               .Accepts(AcceptedCharacters.None),
                        Factory.MetaMarkup("*", HtmlSymbolType.RazorCommentStar)
                               .Accepts(AcceptedCharacters.None),
                        Factory.Span(SpanKind.Comment, new HtmlSymbol(
                            Factory.LocationTracker.CurrentLocation,
                            String.Empty,
                            HtmlSymbolType.Unknown))
                               .Accepts(AcceptedCharacters.Any),
                        Factory.MetaMarkup("*", HtmlSymbolType.RazorCommentStar)
                               .Accepts(AcceptedCharacters.None),
                        Factory.MarkupTransition(HtmlSymbolType.RazorCommentTransition)
                               .Accepts(AcceptedCharacters.None)),
                    Factory.EmptyHtml()));
        }

        [Fact]
        public void RazorCommentInImplicitExpressionMethodCall()
        {
            ParseDocumentTest(@"@foo(@**@",
                new MarkupBlock(
                    Factory.EmptyHtml(),
                    new ExpressionBlock(
                        Factory.CodeTransition(),
                        Factory.Code("foo(")
                               .AsImplicitExpression(VBCodeParser.DefaultKeywords),
                        new CommentBlock(
                            Factory.CodeTransition(VBSymbolType.RazorCommentTransition)
                                   .Accepts(AcceptedCharacters.None),
                            Factory.MetaCode("*", VBSymbolType.RazorCommentStar)
                                   .Accepts(AcceptedCharacters.None),
                            Factory.Span(SpanKind.Comment, new VBSymbol(
                                Factory.LocationTracker.CurrentLocation,
                                String.Empty,
                                VBSymbolType.Unknown))
                                   .Accepts(AcceptedCharacters.Any),
                            Factory.MetaCode("*", VBSymbolType.RazorCommentStar)
                                   .Accepts(AcceptedCharacters.None),
                            Factory.CodeTransition(VBSymbolType.RazorCommentTransition)
                                   .Accepts(AcceptedCharacters.None)),
                        Factory.EmptyVB()
                               .AsImplicitExpression(VBCodeParser.DefaultKeywords))),
                new RazorError(
                    String.Format(RazorResources.ParseError_Expected_CloseBracket_Before_EOF, "(", ")"),
                    4, 0, 4));
        }

        [Fact]
        public void UnterminatedRazorCommentInImplicitExpressionMethodCall()
        {
            ParseDocumentTest("@foo(@*",
                new MarkupBlock(
                    Factory.EmptyHtml(),
                    new ExpressionBlock(
                        Factory.CodeTransition(),
                        Factory.Code("foo(")
                               .AsImplicitExpression(VBCodeParser.DefaultKeywords),
                        new CommentBlock(
                            Factory.CodeTransition(VBSymbolType.RazorCommentTransition)
                                   .Accepts(AcceptedCharacters.None),
                            Factory.MetaCode("*", VBSymbolType.RazorCommentStar)
                                   .Accepts(AcceptedCharacters.None),
                            Factory.Span(SpanKind.Comment, new VBSymbol(
                                Factory.LocationTracker.CurrentLocation,
                                String.Empty,
                                VBSymbolType.Unknown))
                                    .Accepts(AcceptedCharacters.Any)))),
                new RazorError(RazorResources.ParseError_RazorComment_Not_Terminated, 5, 0, 5),
                new RazorError(String.Format(RazorResources.ParseError_Expected_CloseBracket_Before_EOF, "(", ")"), 4, 0, 4));
        }

        [Fact]
        public void RazorCommentInVerbatimBlock()
        {
            ParseDocumentTest("@Code" + Environment.NewLine
                            + "    @<text" + Environment.NewLine
                            + "    @**@" + Environment.NewLine
                            + "End Code",
                new MarkupBlock(
                    Factory.EmptyHtml(),
                    new StatementBlock(
                        Factory.CodeTransition(),
                        Factory.MetaCode("Code").Accepts(AcceptedCharacters.None),
                        Factory.Code("\r\n").AsStatement(),
                        new MarkupBlock(
                            Factory.Markup("    "),
                            Factory.MarkupTransition("@"),
                            Factory.MarkupTransition("<text").Accepts(AcceptedCharacters.Any),
                            Factory.Markup("\r\n    "),
                            new CommentBlock(
                                Factory.MarkupTransition(HtmlSymbolType.RazorCommentTransition)
                                       .Accepts(AcceptedCharacters.None),
                                Factory.MetaMarkup("*", HtmlSymbolType.RazorCommentStar)
                                       .Accepts(AcceptedCharacters.None),
                                Factory.Span(SpanKind.Comment, new HtmlSymbol(
                                    Factory.LocationTracker.CurrentLocation,
                                    String.Empty,
                                    HtmlSymbolType.Unknown))
                                       .Accepts(AcceptedCharacters.Any),
                                Factory.MetaMarkup("*", HtmlSymbolType.RazorCommentStar)
                                       .Accepts(AcceptedCharacters.None),
                                Factory.MarkupTransition(HtmlSymbolType.RazorCommentTransition)
                                       .Accepts(AcceptedCharacters.None)),
                            Factory.Markup("\r\nEnd Code")))),
                new RazorError(RazorResources.ParseError_TextTagCannotContainAttributes, 12, 1, 5),
                new RazorError(String.Format(RazorResources.ParseError_MissingEndTag, "text"), 12, 1, 5),
                new RazorError(String.Format(RazorResources.ParseError_BlockNotTerminated, SyntaxConstants.VB.CodeKeyword, SyntaxConstants.VB.EndCodeKeyword), 1, 0, 1));
        }

        [Fact]
        public void UnterminatedRazorCommentInVerbatimBlock()
        {
            ParseDocumentTest("@Code" + Environment.NewLine
                            + "@*",
                new MarkupBlock(
                    Factory.EmptyHtml(),
                    new StatementBlock(
                        Factory.CodeTransition(),
                        Factory.MetaCode("Code").Accepts(AcceptedCharacters.None),
                        Factory.Code("\r\n")
                               .AsStatement(),
                        new CommentBlock(
                            Factory.CodeTransition(VBSymbolType.RazorCommentTransition)
                                   .Accepts(AcceptedCharacters.None),
                            Factory.MetaCode("*", VBSymbolType.RazorCommentStar)
                                   .Accepts(AcceptedCharacters.None),
                            Factory.Span(SpanKind.Comment, new VBSymbol(Factory.LocationTracker.CurrentLocation,
                                                                        String.Empty,
                                                                        VBSymbolType.Unknown))
                                   .Accepts(AcceptedCharacters.Any)))),
                new RazorError(RazorResources.ParseError_RazorComment_Not_Terminated, 7, 1, 0),
                new RazorError(String.Format(RazorResources.ParseError_BlockNotTerminated, SyntaxConstants.VB.CodeKeyword, SyntaxConstants.VB.EndCodeKeyword), 1, 0, 1));
        }
    }
}
