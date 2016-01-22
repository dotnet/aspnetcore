// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Razor;
using Microsoft.AspNet.Razor.Chunks.Generators;
using Microsoft.AspNet.Razor.Parser;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.Text;
using Microsoft.AspNet.Razor.Tokenizer.Symbols;

namespace Microsoft.AspNet.Mvc.Razor
{
    public static class SpanFactoryExtensions
    {
        public static UnclassifiedCodeSpanConstructor EmptyCSharp(this SpanFactory self)
        {
            var symbol = new CSharpSymbol(self.LocationTracker.CurrentLocation, string.Empty, CSharpSymbolType.Unknown);
            return new UnclassifiedCodeSpanConstructor(self.Span(SpanKind.Code, symbol));
        }

        public static SpanConstructor EmptyHtml(this SpanFactory self)
        {
            var symbol = new HtmlSymbol(self.LocationTracker.CurrentLocation, string.Empty, HtmlSymbolType.Unknown);
            return self.Span(SpanKind.Markup, symbol)
                       .With(new MarkupChunkGenerator());
        }

        public static UnclassifiedCodeSpanConstructor Code(this SpanFactory self, string content)
        {
            return new UnclassifiedCodeSpanConstructor(
                self.Span(SpanKind.Code, content, markup: false));
        }

        public static SpanConstructor CodeTransition(this SpanFactory self)
        {
            return self.Span(SpanKind.Transition, SyntaxConstants.TransitionString, markup: false)
                       .Accepts(AcceptedCharacters.None);
        }

        public static SpanConstructor CodeTransition(this SpanFactory self, string content)
        {
            return self.Span(SpanKind.Transition, content, markup: false).Accepts(AcceptedCharacters.None);
        }

        public static SpanConstructor CodeTransition(this SpanFactory self, CSharpSymbolType type)
        {
            return self.Span(SpanKind.Transition, SyntaxConstants.TransitionString, type)
                       .Accepts(AcceptedCharacters.None);
        }

        public static SpanConstructor CodeTransition(this SpanFactory self, string content, CSharpSymbolType type)
        {
            return self.Span(SpanKind.Transition, content, type).Accepts(AcceptedCharacters.None);
        }

        public static SpanConstructor MarkupTransition(this SpanFactory self)
        {
            return self.Span(SpanKind.Transition, SyntaxConstants.TransitionString, markup: true)
                       .Accepts(AcceptedCharacters.None);
        }

        public static SpanConstructor MarkupTransition(this SpanFactory self, string content)
        {
            return self.Span(SpanKind.Transition, content, markup: true).Accepts(AcceptedCharacters.None);
        }

        public static SpanConstructor MarkupTransition(this SpanFactory self, HtmlSymbolType type)
        {
            return self.Span(SpanKind.Transition, SyntaxConstants.TransitionString, type)
                       .Accepts(AcceptedCharacters.None);
        }

        public static SpanConstructor MarkupTransition(this SpanFactory self, string content, HtmlSymbolType type)
        {
            return self.Span(SpanKind.Transition, content, type).Accepts(AcceptedCharacters.None);
        }

        public static SpanConstructor MetaCode(this SpanFactory self, string content)
        {
            return self.Span(SpanKind.MetaCode, content, markup: false);
        }

        public static SpanConstructor MetaCode(this SpanFactory self, string content, CSharpSymbolType type)
        {
            return self.Span(SpanKind.MetaCode, content, type);
        }

        public static SpanConstructor MetaMarkup(this SpanFactory self, string content)
        {
            return self.Span(SpanKind.MetaCode, content, markup: true);
        }

        public static SpanConstructor MetaMarkup(this SpanFactory self, string content, HtmlSymbolType type)
        {
            return self.Span(SpanKind.MetaCode, content, type);
        }

        public static SpanConstructor Comment(this SpanFactory self, string content, CSharpSymbolType type)
        {
            return self.Span(SpanKind.Comment, content, type);
        }

        public static SpanConstructor Comment(this SpanFactory self, string content, HtmlSymbolType type)
        {
            return self.Span(SpanKind.Comment, content, type);
        }

        public static SpanConstructor Markup(this SpanFactory self, string content)
        {
            return self.Span(SpanKind.Markup, content, markup: true).With(new MarkupChunkGenerator());
        }

        public static SpanConstructor Markup(this SpanFactory self, params string[] content)
        {
            return self.Span(SpanKind.Markup, content, markup: true).With(new MarkupChunkGenerator());
        }

        public static SourceLocation GetLocationAndAdvance(this SourceLocationTracker self, string content)
        {
            var ret = self.CurrentLocation;
            self.UpdateLocation(content);
            return ret;
        }
    }
}