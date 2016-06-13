// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Parser.SyntaxTree;
using Microsoft.AspNetCore.Razor.Text;
using Microsoft.AspNetCore.Razor.Tokenizer;
using Microsoft.AspNetCore.Razor.Tokenizer.Internal;
using Microsoft.AspNetCore.Razor.Tokenizer.Symbols;
using Microsoft.AspNetCore.Razor.Tokenizer.Symbols.Internal;

namespace Microsoft.AspNetCore.Mvc.Razor
{
    public class SpanFactory
    {
        public Func<ITextDocument, ITokenizer> MarkupTokenizerFactory { get; set; }
        public Func<ITextDocument, ITokenizer> CodeTokenizerFactory { get; set; }
        public SourceLocationTracker LocationTracker { get; private set; }

        public static SpanFactory CreateCsHtml()
        {
            return new SpanFactory()
            {
                MarkupTokenizerFactory = doc => new HtmlTokenizer(doc),
                CodeTokenizerFactory = doc => new CSharpTokenizer(doc)
            };
        }

        public SpanFactory()
        {
            LocationTracker = new SourceLocationTracker();
        }

        public SpanConstructor Span(SpanKind kind, string content, CSharpSymbolType type)
        {
            return CreateSymbolSpan(kind, content, st => new CSharpSymbol(st, content, type));
        }

        public SpanConstructor Span(SpanKind kind, string content, HtmlSymbolType type)
        {
            return CreateSymbolSpan(kind, content, st => new HtmlSymbol(st, content, type));
        }

        public SpanConstructor Span(SpanKind kind, string content, bool markup)
        {
            return new SpanConstructor(kind, Tokenize(new[] { content }, markup));
        }

        public SpanConstructor Span(SpanKind kind, string[] content, bool markup)
        {
            return new SpanConstructor(kind, Tokenize(content, markup));
        }

        public SpanConstructor Span(SpanKind kind, params ISymbol[] symbols)
        {
            return new SpanConstructor(kind, symbols);
        }

        private SpanConstructor CreateSymbolSpan(SpanKind kind, string content, Func<SourceLocation, ISymbol> ctor)
        {
            var start = LocationTracker.CurrentLocation;
            LocationTracker.UpdateLocation(content);
            return new SpanConstructor(kind, new[] { ctor(start) });
        }

        public void Reset()
        {
            LocationTracker.CurrentLocation = SourceLocation.Zero;
        }

        private IEnumerable<ISymbol> Tokenize(IEnumerable<string> contentFragments, bool markup)
        {
            return contentFragments.SelectMany(fragment => Tokenize(fragment, markup));
        }

        private IEnumerable<ISymbol> Tokenize(string content, bool markup)
        {
            var tok = MakeTokenizer(markup, new SeekableTextReader(content));
            ISymbol sym;
            ISymbol last = null;
            while ((sym = tok.NextSymbol()) != null)
            {
                OffsetStart(sym, LocationTracker.CurrentLocation);
                last = sym;
                yield return sym;
            }
            LocationTracker.UpdateLocation(content);
        }

        private ITokenizer MakeTokenizer(bool markup, SeekableTextReader seekableTextReader)
        {
            if (markup)
            {
                return MarkupTokenizerFactory(seekableTextReader);
            }
            else
            {
                return CodeTokenizerFactory(seekableTextReader);
            }
        }

        private void OffsetStart(ISymbol sym, SourceLocation sourceLocation)
        {
            sym.OffsetStart(sourceLocation);
        }
    }
}
