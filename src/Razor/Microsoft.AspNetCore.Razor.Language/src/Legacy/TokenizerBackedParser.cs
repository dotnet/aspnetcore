// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language.Syntax.InternalSyntax;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    internal abstract class TokenizerBackedParser<TTokenizer> : ParserBase
        where TTokenizer : Tokenizer
    {
        private readonly SyntaxListPool _pool = new SyntaxListPool();
        private readonly TokenizerView<TTokenizer> _tokenizer;
        private SyntaxListBuilder<SyntaxToken>? _tokenBuilder;

        protected TokenizerBackedParser(LanguageCharacteristics<TTokenizer> language, ParserContext context)
            : base(context)
        {
            Language = language;

            var languageTokenizer = Language.CreateTokenizer(Context.Source);
            _tokenizer = new TokenizerView<TTokenizer>(languageTokenizer);
            SpanContext = new SpanContextBuilder();
        }

        protected SyntaxListPool Pool => _pool;

        protected SyntaxListBuilder<SyntaxToken> TokenBuilder
        {
            get
            {
                if (_tokenBuilder == null)
                {
                    var result = _pool.Allocate<SyntaxToken>();
                    _tokenBuilder = result.Builder;
                }

                return _tokenBuilder.Value;
            }
        }

        protected SpanContextBuilder SpanContext { get; private set; }

        protected Action<SpanContextBuilder> SpanContextConfig { get; set; }

        protected SyntaxToken CurrentToken
        {
            get { return _tokenizer.Current; }
        }

        protected SyntaxToken PreviousToken { get; private set; }

        protected SourceLocation CurrentStart => _tokenizer.Tokenizer.CurrentStart;

        protected bool EndOfFile
        {
            get { return _tokenizer.EndOfFile; }
        }

        protected LanguageCharacteristics<TTokenizer> Language { get; }

        protected SyntaxToken Lookahead(int count)
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }
            else if (count == 0)
            {
                return CurrentToken;
            }

            // We add 1 in order to store the current token.
            var tokens = new SyntaxToken[count + 1];
            var currentToken = CurrentToken;

            tokens[0] = currentToken;

            // We need to look forward "count" many times.
            for (var i = 1; i <= count; i++)
            {
                NextToken();
                tokens[i] = CurrentToken;
            }

            // Restore Tokenizer's location to where it was pointing before the look-ahead.
            for (var i = count; i >= 0; i--)
            {
                PutBack(tokens[i]);
            }

            // The PutBacks above will set CurrentToken to null. EnsureCurrent will set our CurrentToken to the
            // next token.
            EnsureCurrent();

            return tokens[count];
        }

        /// <summary>
        /// Looks forward until the specified condition is met.
        /// </summary>
        /// <param name="condition">A predicate accepting the token being evaluated and the list of tokens which have been looped through.</param>
        /// <returns>true, if the condition was met. false - if the condition wasn't met and the last token has already been processed.</returns>
        /// <remarks>The list of previous tokens is passed in the reverse order. So the last processed element will be the first one in the list.</remarks>
        protected bool LookaheadUntil(Func<SyntaxToken, IEnumerable<SyntaxToken>, bool> condition)
        {
            if (condition == null)
            {
                throw new ArgumentNullException(nameof(condition));
            }

            var matchFound = false;

            var tokens = new List<SyntaxToken>();
            tokens.Add(CurrentToken);

            while (true)
            {
                if (!NextToken())
                {
                    break;
                }

                tokens.Add(CurrentToken);
                if (condition(CurrentToken, tokens))
                {
                    matchFound = true;
                    break;
                }
            }

            // Restore Tokenizer's location to where it was pointing before the look-ahead.
            for (var i = tokens.Count - 1; i >= 0; i--)
            {
                PutBack(tokens[i]);
            }

            // The PutBacks above will set CurrentToken to null. EnsureCurrent will set our CurrentToken to the
            // next token.
            EnsureCurrent();

            return matchFound;
        }

        protected internal bool NextToken()
        {
            PreviousToken = CurrentToken;
            return _tokenizer.Next();
        }

        // Helpers
        [Conditional("DEBUG")]
        internal void Assert(SyntaxKind expectedType)
        {
            Debug.Assert(!EndOfFile && CurrentToken.Kind == expectedType);
        }

        protected internal void PutBack(SyntaxToken token)
        {
            if (token != null)
            {
                _tokenizer.PutBack(token);
            }
        }

        /// <summary>
        /// Put the specified tokens back in the input stream. The provided list MUST be in the ORDER THE TOKENS WERE READ. The
        /// list WILL be reversed and the Putback(SyntaxToken) will be called on each item.
        /// </summary>
        /// <remarks>
        /// If a document contains tokens: a, b, c, d, e, f
        /// and AcceptWhile or AcceptUntil is used to collect until d
        /// the list returned by AcceptWhile/Until will contain: a, b, c IN THAT ORDER
        /// that is the correct format for providing to this method. The caller of this method would,
        /// in that case, want to put c, b and a back into the stream, so "a, b, c" is the CORRECT order
        /// </remarks>
        protected internal void PutBack(IEnumerable<SyntaxToken> tokens)
        {
            foreach (var token in tokens.Reverse())
            {
                PutBack(token);
            }
        }

        protected internal void PutCurrentBack()
        {
            if (!EndOfFile && CurrentToken != null)
            {
                PutBack(CurrentToken);
            }
        }

        protected internal bool NextIs(SyntaxKind type)
        {
            return NextIs(token => token != null && type == token.Kind);
        }

        protected internal bool NextIs(params SyntaxKind[] types)
        {
            return NextIs(token => token != null && types.Any(t => t == token.Kind));
        }

        protected internal bool NextIs(Func<SyntaxToken, bool> condition)
        {
            var cur = CurrentToken;
            if (NextToken())
            {
                var result = condition(CurrentToken);
                PutCurrentBack();
                PutBack(cur);
                EnsureCurrent();
                return result;
            }
            else
            {
                PutBack(cur);
                EnsureCurrent();
            }

            return false;
        }

        protected internal bool Was(SyntaxKind type)
        {
            return PreviousToken != null && PreviousToken.Kind == type;
        }

        protected internal bool At(SyntaxKind type)
        {
            return !EndOfFile && CurrentToken != null && CurrentToken.Kind == type;
        }

        protected bool EnsureCurrent()
        {
            if (CurrentToken == null)
            {
                return NextToken();
            }

            return true;
        }

        protected internal IEnumerable<SyntaxToken> ReadWhile(params SyntaxKind[] types)
        {
            return ReadWhile(token => types.Any(expected => expected == token.Kind));
        }

        protected internal IEnumerable<SyntaxToken> ReadWhile(Func<SyntaxToken, bool> condition)
        {
            return ReadWhileLazy(condition).ToList();
        }

        protected bool AtIdentifier(bool allowKeywords)
        {
            return CurrentToken != null &&
                   (Language.IsIdentifier(CurrentToken) ||
                    (allowKeywords && Language.IsKeyword(CurrentToken)));
        }

        // Don't open this to sub classes because it's lazy but it looks eager.
        // You have to advance the Enumerable to read the next characters.
        internal IEnumerable<SyntaxToken> ReadWhileLazy(Func<SyntaxToken, bool> condition)
        {
            while (EnsureCurrent() && condition(CurrentToken))
            {
                yield return CurrentToken;
                NextToken();
            }
        }

        protected RazorCommentBlockSyntax ParseRazorComment()
        {
            if (!Language.KnowsTokenType(KnownTokenType.CommentStart) ||
                !Language.KnowsTokenType(KnownTokenType.CommentStar) ||
                !Language.KnowsTokenType(KnownTokenType.CommentBody))
            {
                throw new InvalidOperationException(Resources.Language_Does_Not_Support_RazorComment);
            }

            RazorCommentBlockSyntax commentBlock;
            using (PushSpanContextConfig(CommentSpanContextConfig))
            {
                EnsureCurrent();
                var start = CurrentStart;
                Debug.Assert(At(SyntaxKind.RazorCommentTransition));
                var startTransition = EatExpectedToken(SyntaxKind.RazorCommentTransition);
                var startStar = EatExpectedToken(SyntaxKind.RazorCommentStar);
                var comment = GetOptionalToken(SyntaxKind.RazorCommentLiteral);
                if (comment == null)
                {
                    comment = SyntaxFactory.MissingToken(SyntaxKind.RazorCommentLiteral);
                }
                var endStar = GetOptionalToken(SyntaxKind.RazorCommentStar);
                if (endStar == null)
                {
                    var diagnostic = RazorDiagnosticFactory.CreateParsing_RazorCommentNotTerminated(
                        new SourceSpan(start, contentLength: 2 /* @* */));
                    endStar = SyntaxFactory.MissingToken(SyntaxKind.RazorCommentStar, diagnostic);
                    Context.ErrorSink.OnError(diagnostic);
                }
                var endTransition = GetOptionalToken(SyntaxKind.RazorCommentTransition);
                if (endTransition == null)
                {
                    if (!endStar.IsMissing)
                    {
                        var diagnostic = RazorDiagnosticFactory.CreateParsing_RazorCommentNotTerminated(
                            new SourceSpan(start, contentLength: 2 /* @* */));
                        Context.ErrorSink.OnError(diagnostic);
                        endTransition = SyntaxFactory.MissingToken(SyntaxKind.RazorCommentTransition, diagnostic);
                    }

                    endTransition = SyntaxFactory.MissingToken(SyntaxKind.RazorCommentTransition);
                }

                commentBlock = SyntaxFactory.RazorCommentBlock(startTransition, startStar, comment, endStar, endTransition);

                // Make sure we generate a marker symbol after a comment if necessary.
                if (!comment.IsMissing || !endStar.IsMissing || !endTransition.IsMissing)
                {
                    Context.LastAcceptedCharacters = AcceptedCharactersInternal.None;
                }
            }

            InitializeContext(SpanContext);

            return commentBlock;
        }

        private void CommentSpanContextConfig(SpanContextBuilder spanContext)
        {
            spanContext.ChunkGenerator = SpanChunkGenerator.Null;
            spanContext.EditHandler = SpanEditHandler.CreateDefault(Language.TokenizeString);
        }

        protected SyntaxToken EatCurrentToken()
        {
            Debug.Assert(!EndOfFile && CurrentToken != null);
            var token = CurrentToken;
            NextToken();
            return token;
        }

        protected SyntaxToken EatExpectedToken(params SyntaxKind[] kinds)
        {
            Debug.Assert(!EndOfFile && CurrentToken != null && kinds.Contains(CurrentToken.Kind));
            var token = CurrentToken;
            NextToken();
            return token;
        }

        protected SyntaxToken GetOptionalToken(SyntaxKind kind)
        {
            if (At(kind))
            {
                var token = CurrentToken;
                NextToken();
                return token;
            }

            return null;
        }

        protected internal void AcceptWhile(SyntaxKind type)
        {
            AcceptWhile(token => type == token.Kind);
        }

        // We want to avoid array allocations and enumeration where possible, so we use the same technique as string.Format
        protected internal void AcceptWhile(SyntaxKind type1, SyntaxKind type2)
        {
            AcceptWhile(token => type1 == token.Kind || type2 == token.Kind);
        }

        protected internal void AcceptWhile(SyntaxKind type1, SyntaxKind type2, SyntaxKind type3)
        {
            AcceptWhile(token => type1 == token.Kind || type2 == token.Kind || type3 == token.Kind);
        }

        protected internal void AcceptWhile(params SyntaxKind[] types)
        {
            AcceptWhile(token => types.Any(expected => expected == token.Kind));
        }

        protected internal void AcceptUntil(SyntaxKind type)
        {
            AcceptWhile(token => type != token.Kind);
        }

        // We want to avoid array allocations and enumeration where possible, so we use the same technique as string.Format
        protected internal void AcceptUntil(SyntaxKind type1, SyntaxKind type2)
        {
            AcceptWhile(token => type1 != token.Kind && type2 != token.Kind);
        }

        protected internal void AcceptUntil(SyntaxKind type1, SyntaxKind type2, SyntaxKind type3)
        {
            AcceptWhile(token => type1 != token.Kind && type2 != token.Kind && type3 != token.Kind);
        }

        protected internal void AcceptUntil(params SyntaxKind[] types)
        {
            AcceptWhile(token => types.All(expected => expected != token.Kind));
        }

        protected internal void AcceptWhile(Func<SyntaxToken, bool> condition)
        {
            Accept(ReadWhileLazy(condition));
        }

        protected internal void Accept(IEnumerable<SyntaxToken> tokens)
        {
            foreach (var token in tokens)
            {
                Accept(token);
            }
        }

        protected internal void Accept(SyntaxToken token)
        {
            if (token != null)
            {
                if (token.Kind == SyntaxKind.NewLine)
                {
                    Context.StartOfLine = true;
                }
                else if (token.Kind != SyntaxKind.Whitespace)
                {
                    Context.StartOfLine = false;
                }

                foreach (var error in token.GetDiagnostics())
                {
                    Context.ErrorSink.OnError(error);
                }

                TokenBuilder.Add(token);
            }
        }

        protected internal bool AcceptAll(params SyntaxKind[] kinds)
        {
            foreach (var kind in kinds)
            {
                if (CurrentToken == null || CurrentToken.Kind != kind)
                {
                    return false;
                }
                AcceptAndMoveNext();
            }
            return true;
        }

        protected internal bool AcceptAndMoveNext()
        {
            Accept(CurrentToken);
            return NextToken();
        }

        protected SyntaxList<SyntaxToken> Output()
        {
            var list = TokenBuilder.ToList();
            TokenBuilder.Clear();
            return list;
        }

        protected SyntaxToken AcceptWhitespaceInLines()
        {
            SyntaxToken lastWs = null;
            while (Language.IsWhitespace(CurrentToken) || Language.IsNewLine(CurrentToken))
            {
                // Capture the previous whitespace node
                if (lastWs != null)
                {
                    Accept(lastWs);
                }

                if (Language.IsWhitespace(CurrentToken))
                {
                    lastWs = CurrentToken;
                }
                else if (Language.IsNewLine(CurrentToken))
                {
                    // Accept newline and reset last whitespace tracker
                    Accept(CurrentToken);
                    lastWs = null;
                }

                NextToken();
            }

            return lastWs;
        }

        protected internal bool TryAccept(SyntaxKind type)
        {
            if (At(type))
            {
                AcceptAndMoveNext();
                return true;
            }
            return false;
        }

        protected internal void AcceptMarkerTokenIfNecessary()
        {
            if (TokenBuilder.Count == 0 && Context.LastAcceptedCharacters != AcceptedCharactersInternal.Any)
            {
                Accept(Language.CreateMarkerToken());
            }
        }

        protected MarkupTextLiteralSyntax OutputAsMarkupLiteral()
        {
            var tokens = Output();
            if (tokens.Count == 0)
            {
                return null;
            }

            return GetNodeWithSpanContext(SyntaxFactory.MarkupTextLiteral(tokens));
        }

        protected MarkupEphemeralTextLiteralSyntax OutputAsMarkupEphemeralLiteral()
        {
            var tokens = Output();
            if (tokens.Count == 0)
            {
                return null;
            }

            return GetNodeWithSpanContext(SyntaxFactory.MarkupEphemeralTextLiteral(tokens));
        }

        protected RazorMetaCodeSyntax OutputAsMetaCode(SyntaxList<SyntaxToken> tokens, AcceptedCharactersInternal? accepted = null)
        {
            if (tokens.Count == 0)
            {
                return null;
            }

            var metacode = SyntaxFactory.RazorMetaCode(tokens);
            SpanContext.ChunkGenerator = SpanChunkGenerator.Null;
            SpanContext.EditHandler.AcceptedCharacters = accepted ?? AcceptedCharactersInternal.None;

            return GetNodeWithSpanContext(metacode);
        }

        protected TNode GetNodeWithSpanContext<TNode>(TNode node) where TNode : Syntax.GreenNode
        {
            var spanContext = SpanContext.Build();
            Context.LastAcceptedCharacters = spanContext.EditHandler.AcceptedCharacters;
            InitializeContext(SpanContext);
            var annotation = new Syntax.SyntaxAnnotation(SyntaxConstants.SpanContextKind, spanContext);

            return (TNode)node.SetAnnotations(new[] { annotation });
        }

        protected IDisposable PushSpanContextConfig()
        {
            return PushSpanContextConfig(newConfig: (Action<SpanContextBuilder, Action<SpanContextBuilder>>)null);
        }

        protected IDisposable PushSpanContextConfig(Action<SpanContextBuilder> newConfig)
        {
            return PushSpanContextConfig(newConfig == null ? (Action<SpanContextBuilder, Action<SpanContextBuilder>>)null : (span, _) => newConfig(span));
        }

        protected IDisposable PushSpanContextConfig(Action<SpanContextBuilder, Action<SpanContextBuilder>> newConfig)
        {
            var old = SpanContextConfig;
            ConfigureSpanContext(newConfig);
            return new DisposableAction(() => SpanContextConfig = old);
        }

        protected void ConfigureSpanContext(Action<SpanContextBuilder> config)
        {
            SpanContextConfig = config;
            InitializeContext(SpanContext);
        }

        protected void ConfigureSpanContext(Action<SpanContextBuilder, Action<SpanContextBuilder>> config)
        {
            var prev = SpanContextConfig;
            if (config == null)
            {
                SpanContextConfig = null;
            }
            else
            {
                SpanContextConfig = span => config(span, prev);
            }
            InitializeContext(SpanContext);
        }

        protected void InitializeContext(SpanContextBuilder spanContext)
        {
            SpanContextConfig?.Invoke(spanContext);
        }
    }
}
