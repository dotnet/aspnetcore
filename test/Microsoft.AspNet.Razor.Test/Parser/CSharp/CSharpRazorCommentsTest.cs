// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Razor.Parser;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.Test.Framework;
using Microsoft.AspNet.Razor.Tokenizer.Symbols;
using Xunit;

namespace Microsoft.AspNet.Razor.Test.Parser.CSharp
{
    public class CSharpRazorCommentsTest : CsHtmlMarkupParserTestBase
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
                            string.Empty,
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
                            string.Empty,
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
            ParseDocumentTest("@foo(" + Environment.NewLine
                            + "@**@" + Environment.NewLine,
                new MarkupBlock(
                    Factory.EmptyHtml(),
                    new ExpressionBlock(
                        Factory.CodeTransition(),
                        Factory.Code("foo(" + Environment.NewLine)
                               .AsImplicitExpression(CSharpCodeParser.DefaultKeywords),
                        new CommentBlock(
                            Factory.CodeTransition(CSharpSymbolType.RazorCommentTransition)
                                   .Accepts(AcceptedCharacters.None),
                            Factory.MetaCode("*", CSharpSymbolType.RazorCommentStar)
                                   .Accepts(AcceptedCharacters.None),
                            Factory.Span(SpanKind.Comment, new CSharpSymbol(
                                Factory.LocationTracker.CurrentLocation,
                                string.Empty,
                                CSharpSymbolType.Unknown))
                                   .Accepts(AcceptedCharacters.Any),
                            Factory.MetaCode("*", CSharpSymbolType.RazorCommentStar)
                                   .Accepts(AcceptedCharacters.None),
                            Factory.CodeTransition(CSharpSymbolType.RazorCommentTransition)
                                   .Accepts(AcceptedCharacters.None)),
                        Factory.Code(Environment.NewLine)
                               .AsImplicitExpression(CSharpCodeParser.DefaultKeywords))),
                new RazorError(
                    RazorResources.FormatParseError_Expected_CloseBracket_Before_EOF("(", ")"),
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
                               .AsImplicitExpression(CSharpCodeParser.DefaultKeywords),
                        new CommentBlock(
                            Factory.CodeTransition(CSharpSymbolType.RazorCommentTransition)
                                   .Accepts(AcceptedCharacters.None),
                            Factory.MetaCode("*", CSharpSymbolType.RazorCommentStar)
                                   .Accepts(AcceptedCharacters.None),
                            Factory.Span(SpanKind.Comment, new CSharpSymbol(
                                Factory.LocationTracker.CurrentLocation,
                                string.Empty,
                                CSharpSymbolType.Unknown))
                                    .Accepts(AcceptedCharacters.Any)))),
                new RazorError(RazorResources.ParseError_RazorComment_Not_Terminated, 5, 0, 5),
                new RazorError(RazorResources.FormatParseError_Expected_CloseBracket_Before_EOF("(", ")"), 4, 0, 4));
        }

        [Fact]
        public void RazorCommentInVerbatimBlock()
        {
            ParseDocumentTest("@{" + Environment.NewLine
                            + "    <text" + Environment.NewLine
                            + "    @**@" + Environment.NewLine
                            + "}",
                new MarkupBlock(
                    Factory.EmptyHtml(),
                    new StatementBlock(
                        Factory.CodeTransition(),
                        Factory.MetaCode("{").Accepts(AcceptedCharacters.None),
                        Factory.Code(Environment.NewLine)
                            .AsStatement()
                            .AutoCompleteWith("}"),
                        new MarkupBlock(
                            Factory.Markup("    "),
                            new MarkupTagBlock(
                                Factory.MarkupTransition("<text").Accepts(AcceptedCharacters.Any)),
                            Factory.Markup(Environment.NewLine + "    ").Accepts(AcceptedCharacters.None),
                            new CommentBlock(
                                Factory.MarkupTransition(HtmlSymbolType.RazorCommentTransition)
                                       .Accepts(AcceptedCharacters.None),
                                Factory.MetaMarkup("*", HtmlSymbolType.RazorCommentStar)
                                       .Accepts(AcceptedCharacters.None),
                                Factory.Span(SpanKind.Comment, new HtmlSymbol(
                                    Factory.LocationTracker.CurrentLocation,
                                    string.Empty,
                                    HtmlSymbolType.Unknown))
                                       .Accepts(AcceptedCharacters.Any),
                                Factory.MetaMarkup("*", HtmlSymbolType.RazorCommentStar)
                                       .Accepts(AcceptedCharacters.None),
                                Factory.MarkupTransition(HtmlSymbolType.RazorCommentTransition)
                                       .Accepts(AcceptedCharacters.None)),
                            Factory.Markup(Environment.NewLine + "}")))),
                new RazorError(RazorResources.ParseError_TextTagCannotContainAttributes, 6 + Environment.NewLine.Length, 1, 4),
                new RazorError(RazorResources.FormatParseError_MissingEndTag("text"), 6 + Environment.NewLine.Length, 1, 4),
                new RazorError(RazorResources.FormatParseError_Expected_EndOfBlock_Before_EOF(RazorResources.BlockName_Code, "}", "{"), 1, 0, 1));
        }

        [Fact]
        public void UnterminatedRazorCommentInVerbatimBlock()
        {
            ParseDocumentTest("@{@*",
                new MarkupBlock(
                    Factory.EmptyHtml(),
                    new StatementBlock(
                        Factory.CodeTransition(),
                        Factory.MetaCode("{").Accepts(AcceptedCharacters.None),
                        Factory.EmptyCSharp()
                            .AsStatement()
                            .AutoCompleteWith("}"),
                        new CommentBlock(
                            Factory.CodeTransition(CSharpSymbolType.RazorCommentTransition)
                                   .Accepts(AcceptedCharacters.None),
                            Factory.MetaCode("*", CSharpSymbolType.RazorCommentStar)
                                   .Accepts(AcceptedCharacters.None),
                            Factory.Span(SpanKind.Comment, new CSharpSymbol(Factory.LocationTracker.CurrentLocation,
                                                                        string.Empty,
                                                                        CSharpSymbolType.Unknown))
                                   .Accepts(AcceptedCharacters.Any)))),
                new RazorError(RazorResources.ParseError_RazorComment_Not_Terminated, 2, 0, 2),
                new RazorError(RazorResources.FormatParseError_Expected_EndOfBlock_Before_EOF(RazorResources.BlockName_Code, "}", "{"), 1, 0, 1));
        }
    }
}
