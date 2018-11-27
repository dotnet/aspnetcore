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
                    SpanKindInternal.Code,
                    new CSharpToken(string.Empty, CSharpTokenType.Unknown)));
        }

        public static SpanConstructor EmptyHtml(this SpanFactory self)
        {
            return self
                .Span(
                    SpanKindInternal.Markup,
                    new HtmlToken(string.Empty, HtmlTokenType.Unknown))
                .With(new MarkupChunkGenerator());
        }

        public static UnclassifiedCodeSpanConstructor Code(this SpanFactory self, string content)
        {
            return new UnclassifiedCodeSpanConstructor(
                self.Span(SpanKindInternal.Code, content, markup: false));
        }

        public static SpanConstructor CodeTransition(this SpanFactory self)
        {
            return self
                .Span(SpanKindInternal.Transition, SyntaxConstants.TransitionString, markup: false)
                .Accepts(AcceptedCharactersInternal.None);
        }

        public static SpanConstructor CodeTransition(this SpanFactory self, string content)
        {
            return self.Span(SpanKindInternal.Transition, content, markup: false).Accepts(AcceptedCharactersInternal.None);
        }

        public static SpanConstructor CodeTransition(this SpanFactory self, CSharpTokenType type)
        {
            return self
                .Span(SpanKindInternal.Transition, SyntaxConstants.TransitionString, type)
                .Accepts(AcceptedCharactersInternal.None);
        }

        public static SpanConstructor CodeTransition(this SpanFactory self, string content, CSharpTokenType type)
        {
            return self.Span(SpanKindInternal.Transition, content, type).Accepts(AcceptedCharactersInternal.None);
        }

        public static SpanConstructor MarkupTransition(this SpanFactory self)
        {
            return self
                .Span(SpanKindInternal.Transition, SyntaxConstants.TransitionString, markup: true)
                .Accepts(AcceptedCharactersInternal.None);
        }

        public static SpanConstructor MarkupTransition(this SpanFactory self, string content)
        {
            return self.Span(SpanKindInternal.Transition, content, markup: true).Accepts(AcceptedCharactersInternal.None);
        }

        public static SpanConstructor MarkupTransition(this SpanFactory self, HtmlTokenType type)
        {
            return self
                .Span(SpanKindInternal.Transition, SyntaxConstants.TransitionString, type)
                .Accepts(AcceptedCharactersInternal.None);
        }

        public static SpanConstructor MarkupTransition(this SpanFactory self, string content, HtmlTokenType type)
        {
            return self.Span(SpanKindInternal.Transition, content, type).Accepts(AcceptedCharactersInternal.None);
        }

        public static SpanConstructor MetaCode(this SpanFactory self, string content)
        {
            return self.Span(SpanKindInternal.MetaCode, content, markup: false);
        }

        public static SpanConstructor MetaCode(this SpanFactory self, string content, CSharpTokenType type)
        {
            return self.Span(SpanKindInternal.MetaCode, content, type);
        }

        public static SpanConstructor MetaMarkup(this SpanFactory self, string content)
        {
            return self.Span(SpanKindInternal.MetaCode, content, markup: true);
        }

        public static SpanConstructor MetaMarkup(this SpanFactory self, string content, HtmlTokenType type)
        {
            return self.Span(SpanKindInternal.MetaCode, content, type);
        }

        public static SpanConstructor Comment(this SpanFactory self, string content, CSharpTokenType type)
        {
            return self.Span(SpanKindInternal.Comment, content, type);
        }

        public static SpanConstructor Comment(this SpanFactory self, string content, HtmlTokenType type)
        {
            return self.Span(SpanKindInternal.Comment, content, type);
        }

        public static SpanConstructor BangEscape(this SpanFactory self)
        {
            return self
                .Span(SpanKindInternal.MetaCode, "!", markup: true)
                .With(SpanChunkGenerator.Null)
                .Accepts(AcceptedCharactersInternal.None);
        }

        public static SpanConstructor Markup(this SpanFactory self, string content)
        {
            return self.Span(SpanKindInternal.Markup, content, markup: true).With(new MarkupChunkGenerator());
        }

        public static SpanConstructor Markup(this SpanFactory self, params string[] content)
        {
            return self.Span(SpanKindInternal.Markup, content, markup: true).With(new MarkupChunkGenerator());
        }

        public static SpanConstructor CodeMarkup(this SpanFactory self, params string[] content)
        {
            return self
                .Span(SpanKindInternal.Code, content, markup: true)
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
                .Accepts(AcceptedCharactersInternal.AnyExceptNewline);
        }

        public static SpanConstructor AsDirectiveToken(this SpanConstructor self, DirectiveTokenDescriptor descriptor)
        {
            return self
                .With(new DirectiveTokenChunkGenerator(descriptor))
                .With(new DirectiveTokenEditHandler((content) => SpanConstructor.TestTokenizer(content)))
                .Accepts(AcceptedCharactersInternal.NonWhiteSpace);
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


        public SpanConstructor Span(SpanKindInternal kind, string content, CSharpTokenType type)
        {
            return CreateTokenSpan(kind, content, () => new CSharpToken(content, type));
        }

        public SpanConstructor Span(SpanKindInternal kind, string content, HtmlTokenType type)
        {
            return CreateTokenSpan(kind, content, () => new HtmlToken(content, type));
        }

        public SpanConstructor Span(SpanKindInternal kind, string content, bool markup)
        {
            return new SpanConstructor(kind, LocationTracker.CurrentLocation, Tokenize(new[] { content }, markup));
        }

        public SpanConstructor Span(SpanKindInternal kind, string[] content, bool markup)
        {
            return new SpanConstructor(kind, LocationTracker.CurrentLocation, Tokenize(content, markup));
        }

        public SpanConstructor Span(SpanKindInternal kind, params IToken[] tokens)
        {
            var start = LocationTracker.CurrentLocation;
            foreach (var token in tokens)
            {
                LocationTracker.UpdateLocation(token.Content);
            }

            return new SpanConstructor(kind, start, tokens);
        }

        private SpanConstructor CreateTokenSpan(SpanKindInternal kind, string content, Func<IToken> ctor)
        {
            var start = LocationTracker.CurrentLocation;
            LocationTracker.UpdateLocation(content);

            return new SpanConstructor(kind, start, new[] { ctor() });
        }

        public void Reset()
        {
            LocationTracker.CurrentLocation = SourceLocation.Zero;
        }

        private IEnumerable<IToken> Tokenize(IEnumerable<string> contentFragments, bool markup)
        {
            return contentFragments.SelectMany(fragment => Tokenize(fragment, markup));
        }

        private IEnumerable<IToken> Tokenize(string content, bool markup)
        {
            var tokenizer = MakeTokenizer(markup, new SeekableTextReader(content, filePath: null));
            IToken token;
            IToken last = null;

            while ((token = tokenizer.NextToken()) != null)
            {
                last = token;
                yield return token;
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
        public static SpanConstructor Accepts(this SpanConstructor self, AcceptedCharactersInternal accepted)
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
            _self.Builder.Kind = SpanKindInternal.MetaCode;
            return _self;
        }

        public SpanConstructor AsStatement()
        {
            return _self.With(new StatementChunkGenerator());
        }

        public SpanConstructor AsCodeBlock()
        {
            return AsStatement().With(new CodeBlockEditHandler(CSharpLanguageCharacteristics.Instance.TokenizeString));
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

        public SpanConstructor AsAddTagHelper(
            string lookupText,
            string directiveText,
            string typePattern = null,
            string assemblyName = null,
            params RazorDiagnostic[] errors)
        {
            var diagnostics = errors.ToList();
            return _self
                .With(new AddTagHelperChunkGenerator(lookupText, directiveText, typePattern, assemblyName, diagnostics))
                .Accepts(AcceptedCharactersInternal.AnyExceptNewline);
        }

        public SpanConstructor AsRemoveTagHelper(
            string lookupText,
            string directiveText,
            string typePattern = null,
            string assemblyName = null,
            params RazorDiagnostic[] errors)
        {
            var diagnostics = errors.ToList();
            return _self
                .With(new RemoveTagHelperChunkGenerator(lookupText, directiveText, typePattern, assemblyName, diagnostics))
                .Accepts(AcceptedCharactersInternal.AnyExceptNewline);
        }

        public SpanConstructor AsTagHelperPrefixDirective(string prefix, string directiveText, params RazorDiagnostic[] errors)
        {
            var diagnostics = errors.ToList();
            return _self
                .With(new TagHelperPrefixDirectiveChunkGenerator(prefix, directiveText, diagnostics))
                .Accepts(AcceptedCharactersInternal.AnyExceptNewline);
        }

        public SpanConstructor As(ISpanChunkGenerator chunkGenerator)
        {
            return _self.With(chunkGenerator);
        }
    }

    internal class SpanConstructor
    {
        public SpanBuilder Builder { get; private set; }

        internal static IEnumerable<IToken> TestTokenizer(string str)
        {
            yield return new RawTextToken(SourceLocation.Zero, str);
        }

        public SpanConstructor(SpanKindInternal kind, SourceLocation location, IEnumerable<IToken> tokens)
        {
            Builder = new SpanBuilder(location);
            Builder.Kind = kind;
            Builder.EditHandler = SpanEditHandler.CreateDefault((content) => SpanConstructor.TestTokenizer(content));
            foreach (IToken sym in tokens)
            {
                Builder.Accept(sym);
            }
        }

        private Span Build()
        {
            return Builder.Build();
        }

        public SpanConstructor As(SpanKindInternal spanKind)
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
