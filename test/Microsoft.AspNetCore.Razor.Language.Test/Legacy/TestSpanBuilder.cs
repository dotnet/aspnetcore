// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    internal static class SpanFactoryExtensions
    {
        public static UnclassifiedCodeSpanConstructor EmptyCSharp(this SpanFactory self)
        {
            return new UnclassifiedCodeSpanConstructor(
                self.Span(
                    SpanKind.Code,
                    new CSharpSymbol(string.Empty, CSharpSymbolType.Unknown)));
        }

        public static SpanConstructor EmptyHtml(this SpanFactory self)
        {
            return self
                .Span(
                    SpanKind.Markup,
                    new HtmlSymbol(string.Empty, HtmlSymbolType.Unknown))
                .With(new MarkupChunkGenerator());
        }

        public static UnclassifiedCodeSpanConstructor Code(this SpanFactory self, string content)
        {
            return new UnclassifiedCodeSpanConstructor(
                self.Span(SpanKind.Code, content, markup: false));
        }

        public static SpanConstructor CodeTransition(this SpanFactory self)
        {
            return self
                .Span(SpanKind.Transition, SyntaxConstants.TransitionString, markup: false)
                .Accepts(AcceptedCharacters.None);
        }

        public static SpanConstructor CodeTransition(this SpanFactory self, string content)
        {
            return self.Span(SpanKind.Transition, content, markup: false).Accepts(AcceptedCharacters.None);
        }

        public static SpanConstructor CodeTransition(this SpanFactory self, CSharpSymbolType type)
        {
            return self
                .Span(SpanKind.Transition, SyntaxConstants.TransitionString, type)
                .Accepts(AcceptedCharacters.None);
        }

        public static SpanConstructor CodeTransition(this SpanFactory self, string content, CSharpSymbolType type)
        {
            return self.Span(SpanKind.Transition, content, type).Accepts(AcceptedCharacters.None);
        }

        public static SpanConstructor MarkupTransition(this SpanFactory self)
        {
            return self
                .Span(SpanKind.Transition, SyntaxConstants.TransitionString, markup: true)
                .Accepts(AcceptedCharacters.None);
        }

        public static SpanConstructor MarkupTransition(this SpanFactory self, string content)
        {
            return self.Span(SpanKind.Transition, content, markup: true).Accepts(AcceptedCharacters.None);
        }

        public static SpanConstructor MarkupTransition(this SpanFactory self, HtmlSymbolType type)
        {
            return self
                .Span(SpanKind.Transition, SyntaxConstants.TransitionString, type)
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

        public static SpanConstructor BangEscape(this SpanFactory self)
        {
            return self
                .Span(SpanKind.MetaCode, "!", markup: true)
                .With(SpanChunkGenerator.Null)
                .Accepts(AcceptedCharacters.None);
        }

        public static SpanConstructor Markup(this SpanFactory self, string content)
        {
            return self.Span(SpanKind.Markup, content, markup: true).With(new MarkupChunkGenerator());
        }

        public static SpanConstructor Markup(this SpanFactory self, params string[] content)
        {
            return self.Span(SpanKind.Markup, content, markup: true).With(new MarkupChunkGenerator());
        }

        public static SpanConstructor CodeMarkup(this SpanFactory self, params string[] content)
        {
            return self
                .Span(SpanKind.Code, content, markup: true)
                .AsCodeMarkup();
        }

        public static SpanConstructor CSharpCodeMarkup(this SpanFactory self, string content)
        {
            return self.Code(content)
                .AsImplicitExpression(CSharpCodeParser.DefaultKeywords, acceptTrailingDot: true)
                .AsCodeMarkup();
        }

        public static SpanConstructor AsCodeMarkup(this SpanConstructor self)
        {
            return self
                .With(new ImplicitExpressionEditHandler(
                    (content) => SpanConstructor.TestTokenizer(content),
                    CSharpCodeParser.DefaultKeywords,
                    acceptTrailingDot: true))
                .With(new MarkupChunkGenerator())
                .Accepts(AcceptedCharacters.AnyExceptNewline);
        }

        public static SourceLocation GetLocationAndAdvance(this SourceLocationTracker self, string content)
        {
            var ret = self.CurrentLocation;
            self.UpdateLocation(content);
            return ret;
        }
    }

    internal class SpanFactory
    {
        public SpanFactory()
        {
            LocationTracker = new SourceLocationTracker();

            MarkupTokenizerFactory = doc => new HtmlTokenizer(doc);
            CodeTokenizerFactory = doc => new CSharpTokenizer(doc);
        }

        public Func<ITextDocument, HtmlTokenizer> MarkupTokenizerFactory { get; }
        public Func<ITextDocument, CSharpTokenizer> CodeTokenizerFactory { get; }
        public SourceLocationTracker LocationTracker { get; }


        public SpanConstructor Span(SpanKind kind, string content, CSharpSymbolType type)
        {
            return CreateSymbolSpan(kind, content, () => new CSharpSymbol(content, type));
        }

        public SpanConstructor Span(SpanKind kind, string content, HtmlSymbolType type)
        {
            return CreateSymbolSpan(kind, content, () => new HtmlSymbol(content, type));
        }

        public SpanConstructor Span(SpanKind kind, string content, bool markup)
        {
            return new SpanConstructor(kind, LocationTracker.CurrentLocation, Tokenize(new[] { content }, markup));
        }

        public SpanConstructor Span(SpanKind kind, string[] content, bool markup)
        {
            return new SpanConstructor(kind, LocationTracker.CurrentLocation, Tokenize(content, markup));
        }

        public SpanConstructor Span(SpanKind kind, params ISymbol[] symbols)
        {
            var start = LocationTracker.CurrentLocation;
            foreach (var symbol in symbols)
            {
                LocationTracker.UpdateLocation(symbol.Content);
            }

            return new SpanConstructor(kind, start, symbols);
        }

        private SpanConstructor CreateSymbolSpan(SpanKind kind, string content, Func<ISymbol> ctor)
        {
            var start = LocationTracker.CurrentLocation;
            LocationTracker.UpdateLocation(content);

            return new SpanConstructor(kind, start, new[] { ctor() });
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
            var tokenizer = MakeTokenizer(markup, new SeekableTextReader(content, filePath: null));
            ISymbol symbol;
            ISymbol last = null;

            while ((symbol = tokenizer.NextSymbol()) != null)
            {
                last = symbol;
                yield return symbol;
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
    }

    internal static class SpanConstructorExtensions
    {
        public static SpanConstructor Accepts(this SpanConstructor self, AcceptedCharacters accepted)
        {
            return self.With(eh => eh.AcceptedCharacters = accepted);
        }

        public static SpanConstructor AutoCompleteWith(this SpanConstructor self, string autoCompleteString)
        {
            return AutoCompleteWith(self, autoCompleteString, atEndOfSpan: false);
        }

        public static SpanConstructor AutoCompleteWith(
            this SpanConstructor self,
            string autoCompleteString,
            bool atEndOfSpan)
        {
            return self.With(new AutoCompleteEditHandler(
                (content) => SpanConstructor.TestTokenizer(content),
                autoCompleteAtEndOfSpan: atEndOfSpan)
            {
                AutoCompleteString = autoCompleteString
            });
        }
    }

    internal class UnclassifiedCodeSpanConstructor
    {
        SpanConstructor _self;

        public UnclassifiedCodeSpanConstructor(SpanConstructor self)
        {
            _self = self;
        }

        public SpanConstructor AsMetaCode()
        {
            _self.Builder.Kind = SpanKind.MetaCode;
            return _self;
        }

        public SpanConstructor AsStatement()
        {
            return _self.With(new StatementChunkGenerator());
        }

        public SpanConstructor AsExpression()
        {
            return _self.With(new ExpressionChunkGenerator());
        }

        public SpanConstructor AsImplicitExpression(ISet<string> keywords)
        {
            return AsImplicitExpression(keywords, acceptTrailingDot: false);
        }

        public SpanConstructor AsImplicitExpression(ISet<string> keywords, bool acceptTrailingDot)
        {
            return _self
                .With(new ImplicitExpressionEditHandler((content) => SpanConstructor.TestTokenizer(content), keywords, acceptTrailingDot))
                .With(new ExpressionChunkGenerator());
        }

        public SpanConstructor AsNamespaceImport(string ns)
        {
            return _self.With(new AddImportChunkGenerator(ns));
        }

        public SpanConstructor Hidden()
        {
            return _self.With(SpanChunkGenerator.Null);
        }

        public SpanConstructor AsAddTagHelper(string lookupText)
        {
            return _self
                .With(new AddTagHelperChunkGenerator(lookupText))
                .Accepts(AcceptedCharacters.AnyExceptNewline);
        }

        public SpanConstructor AsRemoveTagHelper(string lookupText)
        {
            return _self
                .With(new RemoveTagHelperChunkGenerator(lookupText))
                .Accepts(AcceptedCharacters.AnyExceptNewline);
        }

        public SpanConstructor AsTagHelperPrefixDirective(string prefix)
        {
            return _self
                .With(new TagHelperPrefixDirectiveChunkGenerator(prefix))
                .Accepts(AcceptedCharacters.AnyExceptNewline);
        }

        public SpanConstructor As(ISpanChunkGenerator chunkGenerator)
        {
            return _self.With(chunkGenerator);
        }
    }

    internal class SpanConstructor
    {
        public SpanBuilder Builder { get; private set; }

        internal static IEnumerable<ISymbol> TestTokenizer(string str)
        {
            yield return new RawTextSymbol(SourceLocation.Zero, str);
        }

        public SpanConstructor(SpanKind kind, SourceLocation location, IEnumerable<ISymbol> symbols)
        {
            Builder = new SpanBuilder(location);
            Builder.Kind = kind;
            Builder.EditHandler = SpanEditHandler.CreateDefault((content) => SpanConstructor.TestTokenizer(content));
            foreach (ISymbol sym in symbols)
            {
                Builder.Accept(sym);
            }
        }

        private Span Build()
        {
            return Builder.Build();
        }

        public SpanConstructor As(SpanKind spanKind)
        {
            Builder.Kind = spanKind;
            return this;
        }

        public SpanConstructor With(ISpanChunkGenerator generator)
        {
            Builder.ChunkGenerator = generator;
            return this;
        }

        public SpanConstructor With(SpanEditHandler handler)
        {
            Builder.EditHandler = handler;
            return this;
        }

        public SpanConstructor With(Action<ISpanChunkGenerator> generatorConfigurer)
        {
            generatorConfigurer(Builder.ChunkGenerator);
            return this;
        }

        public SpanConstructor With(Action<SpanEditHandler> handlerConfigurer)
        {
            handlerConfigurer(Builder.EditHandler);
            return this;
        }

        public static implicit operator Span(SpanConstructor self)
        {
            return self.Build();
        }

        public SpanConstructor Hidden()
        {
            Builder.ChunkGenerator = SpanChunkGenerator.Null;
            return this;
        }
    }
}
