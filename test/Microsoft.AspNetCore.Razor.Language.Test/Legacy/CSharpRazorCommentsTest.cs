// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
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
                               .Accepts(AcceptedCharactersInternal.None),
                        Factory.MetaMarkup("*", HtmlSymbolType.RazorCommentStar)
                               .Accepts(AcceptedCharactersInternal.None),
                        Factory.Span(
                            SpanKindInternal.Comment, 
                            new HtmlSymbol(
                                string.Empty,
                                HtmlSymbolType.Unknown))
                               .Accepts(AcceptedCharactersInternal.Any))),
                RazorDiagnostic.Create(new RazorError(
                    LegacyResources.ParseError_RazorComment_Not_Terminated,
                    SourceLocation.Zero,
                    length: 2)));
        }

        [Fact]
        public void EmptyRazorComment()
        {
            ParseDocumentTest("@**@",
                new MarkupBlock(
                    Factory.EmptyHtml(),
                    new CommentBlock(
                        Factory.MarkupTransition(HtmlSymbolType.RazorCommentTransition)
                               .Accepts(AcceptedCharactersInternal.None),
                        Factory.MetaMarkup("*", HtmlSymbolType.RazorCommentStar)
                               .Accepts(AcceptedCharactersInternal.None),
                        Factory.Span(SpanKindInternal.Comment, new HtmlSymbol(
                            string.Empty,
                            HtmlSymbolType.Unknown))
                               .Accepts(AcceptedCharactersInternal.Any),
                        Factory.MetaMarkup("*", HtmlSymbolType.RazorCommentStar)
                               .Accepts(AcceptedCharactersInternal.None),
                        Factory.MarkupTransition(HtmlSymbolType.RazorCommentTransition)
                               .Accepts(AcceptedCharactersInternal.None)),
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
                                   .Accepts(AcceptedCharactersInternal.None),
                            Factory.MetaCode("*", CSharpSymbolType.RazorCommentStar)
                                   .Accepts(AcceptedCharactersInternal.None),
                            Factory.Span(SpanKindInternal.Comment, new CSharpSymbol(
                                string.Empty,
                                CSharpSymbolType.Unknown))
                                   .Accepts(AcceptedCharactersInternal.Any),
                            Factory.MetaCode("*", CSharpSymbolType.RazorCommentStar)
                                   .Accepts(AcceptedCharactersInternal.None),
                            Factory.CodeTransition(CSharpSymbolType.RazorCommentTransition)
                                   .Accepts(AcceptedCharactersInternal.None)),
                        Factory.Code(Environment.NewLine)
                               .AsImplicitExpression(CSharpCodeParser.DefaultKeywords))),
                RazorDiagnostic.Create(new RazorError(
                    LegacyResources.FormatParseError_Expected_CloseBracket_Before_EOF("(", ")"),
                    new SourceLocation(4, 0, 4),
                    length: 1)));
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
                                   .Accepts(AcceptedCharactersInternal.None),
                            Factory.MetaCode("*", CSharpSymbolType.RazorCommentStar)
                                   .Accepts(AcceptedCharactersInternal.None),
                            Factory.Span(SpanKindInternal.Comment, new CSharpSymbol(
                                string.Empty,
                                CSharpSymbolType.Unknown))
                                    .Accepts(AcceptedCharactersInternal.Any)))),
                RazorDiagnostic.Create(new RazorError(
                    LegacyResources.ParseError_RazorComment_Not_Terminated,
                    new SourceLocation(5, 0, 5),
                    length: 2)),
                RazorDiagnostic.Create(new RazorError(
                    LegacyResources.FormatParseError_Expected_CloseBracket_Before_EOF("(", ")"),
                    new SourceLocation(4, 0, 4),
                    length: 1)));
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
                        Factory.MetaCode("{").Accepts(AcceptedCharactersInternal.None),
                        Factory.Code($"{Environment.NewLine}    ")
                            .AsStatement()
                            .AutoCompleteWith("}"),
                        new MarkupBlock(
                            new MarkupTagBlock(
                                Factory.MarkupTransition("<text").Accepts(AcceptedCharactersInternal.Any)),
                            Factory.Markup(Environment.NewLine).Accepts(AcceptedCharactersInternal.None),
                            Factory.Markup("    ").With(SpanChunkGenerator.Null),
                            new CommentBlock(
                                Factory.MarkupTransition(HtmlSymbolType.RazorCommentTransition)
                                       .Accepts(AcceptedCharactersInternal.None),
                                Factory.MetaMarkup("*", HtmlSymbolType.RazorCommentStar)
                                       .Accepts(AcceptedCharactersInternal.None),
                                Factory.Span(SpanKindInternal.Comment, new HtmlSymbol(
                                    string.Empty,
                                    HtmlSymbolType.Unknown))
                                       .Accepts(AcceptedCharactersInternal.Any),
                                Factory.MetaMarkup("*", HtmlSymbolType.RazorCommentStar)
                                       .Accepts(AcceptedCharactersInternal.None),
                                Factory.MarkupTransition(HtmlSymbolType.RazorCommentTransition)
                                       .Accepts(AcceptedCharactersInternal.None)),
                            Factory.Markup(Environment.NewLine).With(SpanChunkGenerator.Null),
                            Factory.Markup("}")))),
                RazorDiagnostic.Create(new RazorError(
                    LegacyResources.ParseError_TextTagCannotContainAttributes,
                    new SourceLocation(7 + Environment.NewLine.Length, 1, 5),
                    length: 4)),
                RazorDiagnostic.Create(new RazorError(
                    LegacyResources.FormatParseError_MissingEndTag("text"),
                    new SourceLocation(7 + Environment.NewLine.Length, 1, 5),
                    length: 4)),
                RazorDiagnosticFactory.CreateParsing_ExpectedEndOfBlockBeforeEOF(
                    new SourceSpan(new SourceLocation(1, 0, 1), contentLength: 1), LegacyResources.BlockName_Code, "}", "{"));
        }

        [Fact]
        public void UnterminatedRazorCommentInVerbatimBlock()
        {
            ParseDocumentTest("@{@*",
                new MarkupBlock(
                    Factory.EmptyHtml(),
                    new StatementBlock(
                        Factory.CodeTransition(),
                        Factory.MetaCode("{").Accepts(AcceptedCharactersInternal.None),
                        Factory.EmptyCSharp()
                            .AsStatement()
                            .AutoCompleteWith("}"),
                        new CommentBlock(
                            Factory.CodeTransition(CSharpSymbolType.RazorCommentTransition)
                                   .Accepts(AcceptedCharactersInternal.None),
                            Factory.MetaCode("*", CSharpSymbolType.RazorCommentStar)
                                   .Accepts(AcceptedCharactersInternal.None),
                            Factory.Span(SpanKindInternal.Comment, new CSharpSymbol(string.Empty, CSharpSymbolType.Unknown))
                                   .Accepts(AcceptedCharactersInternal.Any)))),
                RazorDiagnostic.Create(new RazorError(
                    LegacyResources.ParseError_RazorComment_Not_Terminated,
                    new SourceLocation(2, 0, 2),
                    length: 2)),
                RazorDiagnosticFactory.CreateParsing_ExpectedEndOfBlockBeforeEOF(
                    new SourceSpan(new SourceLocation(1, 0, 1), contentLength: 1), LegacyResources.BlockName_Code, "}", "{"));
        }

        [Fact]
        public void RazorCommentInMarkup()
        {
            ParseDocumentTest(
                "<p>" + Environment.NewLine
                + "@**@" + Environment.NewLine
                + "</p>",
                new MarkupBlock(
                    new MarkupTagBlock(
                        Factory.Markup("<p>")),
                    Factory.Markup(Environment.NewLine),
                    new CommentBlock(
                        Factory.MarkupTransition(HtmlSymbolType.RazorCommentTransition)
                               .Accepts(AcceptedCharactersInternal.None),
                        Factory.MetaMarkup("*", HtmlSymbolType.RazorCommentStar)
                               .Accepts(AcceptedCharactersInternal.None),
                        Factory.Span(SpanKindInternal.Comment, new HtmlSymbol(
                            string.Empty,
                            HtmlSymbolType.Unknown))
                               .Accepts(AcceptedCharactersInternal.Any),
                        Factory.MetaMarkup("*", HtmlSymbolType.RazorCommentStar)
                               .Accepts(AcceptedCharactersInternal.None),
                        Factory.MarkupTransition(HtmlSymbolType.RazorCommentTransition)
                               .Accepts(AcceptedCharactersInternal.None)),
                    Factory.Markup(Environment.NewLine).With(SpanChunkGenerator.Null),
                    new MarkupTagBlock(
                        Factory.Markup("</p>"))
                    ));
        }

        [Fact]
        public void MultipleRazorCommentInMarkup()
        {
            ParseDocumentTest(
                "<p>" + Environment.NewLine
                + "  @**@  " + Environment.NewLine
                + "@**@" + Environment.NewLine
                + "</p>",
                new MarkupBlock(
                    new MarkupTagBlock(
                        Factory.Markup("<p>")),
                    Factory.Markup(Environment.NewLine),
                    Factory.Markup("  ").With(SpanChunkGenerator.Null),
                    new CommentBlock(
                        Factory.MarkupTransition(HtmlSymbolType.RazorCommentTransition)
                               .Accepts(AcceptedCharactersInternal.None),
                        Factory.MetaMarkup("*", HtmlSymbolType.RazorCommentStar)
                               .Accepts(AcceptedCharactersInternal.None),
                        Factory.Span(SpanKindInternal.Comment, new HtmlSymbol(
                            string.Empty,
                            HtmlSymbolType.Unknown))
                               .Accepts(AcceptedCharactersInternal.Any),
                        Factory.MetaMarkup("*", HtmlSymbolType.RazorCommentStar)
                               .Accepts(AcceptedCharactersInternal.None),
                        Factory.MarkupTransition(HtmlSymbolType.RazorCommentTransition)
                               .Accepts(AcceptedCharactersInternal.None)),
                    Factory.Markup("  " + Environment.NewLine).With(SpanChunkGenerator.Null),
                    new CommentBlock(
                        Factory.MarkupTransition(HtmlSymbolType.RazorCommentTransition)
                               .Accepts(AcceptedCharactersInternal.None),
                        Factory.MetaMarkup("*", HtmlSymbolType.RazorCommentStar)
                               .Accepts(AcceptedCharactersInternal.None),
                        Factory.Span(SpanKindInternal.Comment, new HtmlSymbol(
                            string.Empty,
                            HtmlSymbolType.Unknown))
                               .Accepts(AcceptedCharactersInternal.Any),
                        Factory.MetaMarkup("*", HtmlSymbolType.RazorCommentStar)
                               .Accepts(AcceptedCharactersInternal.None),
                        Factory.MarkupTransition(HtmlSymbolType.RazorCommentTransition)
                               .Accepts(AcceptedCharactersInternal.None)),
                    Factory.Markup(Environment.NewLine).With(SpanChunkGenerator.Null),
                    new MarkupTagBlock(
                        Factory.Markup("</p>"))
                    ));
        }

        [Fact]
        public void MultipleRazorCommentsInSameLineInMarkup()
        {
            ParseDocumentTest(
                "<p>" + Environment.NewLine
                + "@**@  @**@" + Environment.NewLine
                + "</p>",
                new MarkupBlock(
                    new MarkupTagBlock(
                        Factory.Markup("<p>")),
                    Factory.Markup(Environment.NewLine),
                    new CommentBlock(
                        Factory.MarkupTransition(HtmlSymbolType.RazorCommentTransition)
                               .Accepts(AcceptedCharactersInternal.None),
                        Factory.MetaMarkup("*", HtmlSymbolType.RazorCommentStar)
                               .Accepts(AcceptedCharactersInternal.None),
                        Factory.Span(SpanKindInternal.Comment, new HtmlSymbol(
                            string.Empty,
                            HtmlSymbolType.Unknown))
                               .Accepts(AcceptedCharactersInternal.Any),
                        Factory.MetaMarkup("*", HtmlSymbolType.RazorCommentStar)
                               .Accepts(AcceptedCharactersInternal.None),
                        Factory.MarkupTransition(HtmlSymbolType.RazorCommentTransition)
                               .Accepts(AcceptedCharactersInternal.None)),
                    Factory.EmptyHtml(),
                    Factory.Markup("  ").With(SpanChunkGenerator.Null),
                    new CommentBlock(
                        Factory.MarkupTransition(HtmlSymbolType.RazorCommentTransition)
                               .Accepts(AcceptedCharactersInternal.None),
                        Factory.MetaMarkup("*", HtmlSymbolType.RazorCommentStar)
                               .Accepts(AcceptedCharactersInternal.None),
                        Factory.Span(SpanKindInternal.Comment, new HtmlSymbol(
                            string.Empty,
                            HtmlSymbolType.Unknown))
                               .Accepts(AcceptedCharactersInternal.Any),
                        Factory.MetaMarkup("*", HtmlSymbolType.RazorCommentStar)
                               .Accepts(AcceptedCharactersInternal.None),
                        Factory.MarkupTransition(HtmlSymbolType.RazorCommentTransition)
                               .Accepts(AcceptedCharactersInternal.None)),
                    Factory.Markup(Environment.NewLine).With(SpanChunkGenerator.Null),
                    new MarkupTagBlock(
                        Factory.Markup("</p>"))
                    ));
        }

        [Fact]
        public void RazorCommentsSurroundingMarkup()
        {
            ParseDocumentTest(
                "<p>" + Environment.NewLine
                + "@* hello *@ content @* world *@" + Environment.NewLine
                + "</p>",
                new MarkupBlock(
                    new MarkupTagBlock(
                        Factory.Markup("<p>")),
                    Factory.Markup(Environment.NewLine),
                    new CommentBlock(
                        Factory.MarkupTransition(HtmlSymbolType.RazorCommentTransition)
                               .Accepts(AcceptedCharactersInternal.None),
                        Factory.MetaMarkup("*", HtmlSymbolType.RazorCommentStar)
                               .Accepts(AcceptedCharactersInternal.None),
                        Factory.Span(SpanKindInternal.Comment, new HtmlSymbol(
                            " hello ",
                            HtmlSymbolType.RazorComment))
                               .Accepts(AcceptedCharactersInternal.Any),
                        Factory.MetaMarkup("*", HtmlSymbolType.RazorCommentStar)
                               .Accepts(AcceptedCharactersInternal.None),
                        Factory.MarkupTransition(HtmlSymbolType.RazorCommentTransition)
                               .Accepts(AcceptedCharactersInternal.None)),
                    Factory.Markup(" content "),
                    new CommentBlock(
                        Factory.MarkupTransition(HtmlSymbolType.RazorCommentTransition)
                               .Accepts(AcceptedCharactersInternal.None),
                        Factory.MetaMarkup("*", HtmlSymbolType.RazorCommentStar)
                               .Accepts(AcceptedCharactersInternal.None),
                        Factory.Span(SpanKindInternal.Comment, new HtmlSymbol(
                            " world ",
                            HtmlSymbolType.RazorComment))
                               .Accepts(AcceptedCharactersInternal.Any),
                        Factory.MetaMarkup("*", HtmlSymbolType.RazorCommentStar)
                               .Accepts(AcceptedCharactersInternal.None),
                        Factory.MarkupTransition(HtmlSymbolType.RazorCommentTransition)
                               .Accepts(AcceptedCharactersInternal.None)),
                    Factory.Markup(Environment.NewLine),
                    new MarkupTagBlock(
                        Factory.Markup("</p>"))
                    ));
        }

        [Fact]
        public void RazorCommentWithExtraNewLineInMarkup()
        {
            ParseDocumentTest(
                "<p>" + Environment.NewLine + Environment.NewLine
                + "@* content *@" + Environment.NewLine
                + "@*" + Environment.NewLine
                + "content" + Environment.NewLine
                + "*@" + Environment.NewLine + Environment.NewLine
                + "</p>",
                new MarkupBlock(
                    new MarkupTagBlock(
                        Factory.Markup("<p>")),
                    Factory.Markup(Environment.NewLine + Environment.NewLine),
                    new CommentBlock(
                        Factory.MarkupTransition(HtmlSymbolType.RazorCommentTransition)
                               .Accepts(AcceptedCharactersInternal.None),
                        Factory.MetaMarkup("*", HtmlSymbolType.RazorCommentStar)
                               .Accepts(AcceptedCharactersInternal.None),
                        Factory.Span(SpanKindInternal.Comment, new HtmlSymbol(
                            " content ",
                            HtmlSymbolType.RazorComment))
                               .Accepts(AcceptedCharactersInternal.Any),
                        Factory.MetaMarkup("*", HtmlSymbolType.RazorCommentStar)
                               .Accepts(AcceptedCharactersInternal.None),
                        Factory.MarkupTransition(HtmlSymbolType.RazorCommentTransition)
                               .Accepts(AcceptedCharactersInternal.None)),
                    Factory.Markup(Environment.NewLine).With(SpanChunkGenerator.Null),
                    new CommentBlock(
                        Factory.MarkupTransition(HtmlSymbolType.RazorCommentTransition)
                               .Accepts(AcceptedCharactersInternal.None),
                        Factory.MetaMarkup("*", HtmlSymbolType.RazorCommentStar)
                               .Accepts(AcceptedCharactersInternal.None),
                        Factory.Span(SpanKindInternal.Comment, new HtmlSymbol(
                            Environment.NewLine + "content" + Environment.NewLine,
                            HtmlSymbolType.RazorComment))
                               .Accepts(AcceptedCharactersInternal.Any),
                        Factory.MetaMarkup("*", HtmlSymbolType.RazorCommentStar)
                               .Accepts(AcceptedCharactersInternal.None),
                        Factory.MarkupTransition(HtmlSymbolType.RazorCommentTransition)
                               .Accepts(AcceptedCharactersInternal.None)),
                    Factory.Markup(Environment.NewLine).With(SpanChunkGenerator.Null),
                    Factory.Markup(Environment.NewLine),
                    new MarkupTagBlock(
                        Factory.Markup("</p>"))
                    ));
        }
    }
}
