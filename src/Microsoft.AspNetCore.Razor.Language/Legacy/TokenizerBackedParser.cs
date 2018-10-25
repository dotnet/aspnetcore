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
        private readonly TokenizerView<TTokenizer> _tokenizer;

        protected TokenizerBackedParser(LanguageCharacteristics<TTokenizer> language, ParserContext context)
            : base(context)
        {
            Language = language;

            var languageTokenizer = Language.CreateTokenizer(Context.Source);
            _tokenizer = new TokenizerView<TTokenizer>(languageTokenizer);
            Span = new SpanBuilder(CurrentLocation);
        }

        protected ParserState ParserState { get; set; }

        protected SpanBuilder Span { get; private set; }

        protected Action<SpanBuilder> SpanConfig { get; set; }

        protected SyntaxToken CurrentToken
        {
            get { return _tokenizer.Current; }
        }

        protected SyntaxToken PreviousToken { get; private set; }

        protected SourceLocation CurrentLocation => _tokenizer.Tokenizer.CurrentLocation;

        protected SourceLocation CurrentStart => _tokenizer.Tokenizer.CurrentStart;

        protected bool EndOfFile
        {
            get { return _tokenizer.EndOfFile; }
        }

        protected LanguageCharacteristics<TTokenizer> Language { get; }

        protected virtual void HandleEmbeddedTransition()
        {
        }

        protected virtual bool IsAtEmbeddedTransition(bool allowTemplatesAndComments, bool allowTransitions)
        {
            return false;
        }

        public override void BuildSpan(SpanBuilder span, SourceLocation start, string content)
        {
            foreach (var token in Language.TokenizeString(start, content))
            {
                span.Accept(token);
            }
        }

        protected void Initialize(SpanBuilder span)
        {
            SpanConfig?.Invoke(span);
        }

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
            Debug.Assert(!EndOfFile && TokenKindEquals(CurrentToken.Kind, expectedType));
        }

        protected abstract bool TokenKindEquals(SyntaxKind x, SyntaxKind y);

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

        protected internal bool Balance(BalancingModes mode)
        {
            var left = CurrentToken.Kind;
            var right = Language.FlipBracket(left);
            var start = CurrentStart;
            AcceptAndMoveNext();
            if (EndOfFile && ((mode & BalancingModes.NoErrorOnFailure) != BalancingModes.NoErrorOnFailure))
            {
                Context.ErrorSink.OnError(
                    RazorDiagnosticFactory.CreateParsing_ExpectedCloseBracketBeforeEOF(
                        new SourceSpan(start, contentLength: 1 /* { OR } */),
                        Language.GetSample(left),
                        Language.GetSample(right)));
            }

            return Balance(mode, left, right, start);
        }

        protected internal bool Balance(BalancingModes mode, SyntaxKind left, SyntaxKind right, SourceLocation start)
        {
            var startPosition = CurrentStart.AbsoluteIndex;
            var nesting = 1;
            if (!EndOfFile)
            {
                var tokens = new List<SyntaxToken>();
                do
                {
                    if (IsAtEmbeddedTransition(
                        (mode & BalancingModes.AllowCommentsAndTemplates) == BalancingModes.AllowCommentsAndTemplates,
                        (mode & BalancingModes.AllowEmbeddedTransitions) == BalancingModes.AllowEmbeddedTransitions))
                    {
                        Accept(tokens);
                        tokens.Clear();
                        HandleEmbeddedTransition();

                        // Reset backtracking since we've already outputted some spans.
                        startPosition = CurrentStart.AbsoluteIndex;
                    }
                    if (At(left))
                    {
                        nesting++;
                    }
                    else if (At(right))
                    {
                        nesting--;
                    }
                    if (nesting > 0)
                    {
                        tokens.Add(CurrentToken);
                    }
                }
                while (nesting > 0 && NextToken());

                if (nesting > 0)
                {
                    if ((mode & BalancingModes.NoErrorOnFailure) != BalancingModes.NoErrorOnFailure)
                    {
                        Context.ErrorSink.OnError(
                            RazorDiagnosticFactory.CreateParsing_ExpectedCloseBracketBeforeEOF(
                                new SourceSpan(start, contentLength: 1 /* { OR } */),
                                Language.GetSample(left),
                                Language.GetSample(right)));
                    }
                    if ((mode & BalancingModes.BacktrackOnFailure) == BalancingModes.BacktrackOnFailure)
                    {
                        Context.Source.Position = startPosition;
                        NextToken();
                    }
                    else
                    {
                        Accept(tokens);
                    }
                }
                else
                {
                    // Accept all the tokens we saw
                    Accept(tokens);
                }
            }
            return nesting == 0;
        }

        protected internal bool NextIs(SyntaxKind type)
        {
            return NextIs(token => token != null && TokenKindEquals(type, token.Kind));
        }

        protected internal bool NextIs(params SyntaxKind[] types)
        {
            return NextIs(token => token != null && types.Any(t => TokenKindEquals(t, token.Kind)));
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
            return PreviousToken != null && TokenKindEquals(PreviousToken.Kind, type);
        }

        protected internal bool At(SyntaxKind type)
        {
            return !EndOfFile && CurrentToken != null && TokenKindEquals(CurrentToken.Kind, type);
        }

        protected internal bool AcceptAndMoveNext()
        {
            Accept(CurrentToken);
            return NextToken();
        }

        protected SyntaxToken AcceptSingleWhiteSpaceCharacter()
        {
            if (Language.IsWhiteSpace(CurrentToken))
            {
                var pair = Language.SplitToken(CurrentToken, 1, Language.GetKnownTokenType(KnownTokenType.WhiteSpace));
                Accept(pair.Item1);
                Span.EditHandler.AcceptedCharacters = AcceptedCharactersInternal.None;
                NextToken();
                return pair.Item2;
            }
            return null;
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
                foreach (var error in token.GetDiagnostics())
                {
                    Context.ErrorSink.OnError(error);
                }

                Span.Accept(token);
            }
        }

        protected internal bool AcceptAll(params SyntaxKind[] kinds)
        {
            foreach (var kind in kinds)
            {
                if (CurrentToken == null || !TokenKindEquals(CurrentToken.Kind, kind))
                {
                    return false;
                }
                AcceptAndMoveNext();
            }
            return true;
        }

        protected internal void AddMarkerTokenIfNecessary()
        {
            if (Span.Tokens.Count == 0 && Context.Builder.LastAcceptedCharacters != AcceptedCharactersInternal.Any)
            {
                Accept(Language.CreateMarkerToken());
            }
        }

        protected internal void Output(SpanKindInternal kind, SyntaxKind syntaxKind = SyntaxKind.Unknown)
        {
            Configure(kind, null);
            Output(syntaxKind);
        }

        protected internal void Output(SpanKindInternal kind, AcceptedCharactersInternal accepts, SyntaxKind syntaxKind = SyntaxKind.Unknown)
        {
            Configure(kind, accepts);
            Output(syntaxKind);
        }

        protected internal void Output(AcceptedCharactersInternal accepts, SyntaxKind syntaxKind = SyntaxKind.Unknown)
        {
            Configure(null, accepts);
            Output(syntaxKind);
        }

        private void Output(SyntaxKind syntaxKind)
        {
            if (Span.Tokens.Count > 0)
            {
                var nextStart = Span.End;

                var builtSpan = Span.Build(syntaxKind);
                Context.Builder.Add(builtSpan);
                Initialize(Span);

                // Ensure spans are contiguous.
                //
                // Note: Using Span.End here to avoid CurrentLocation. CurrentLocation will
                // vary depending on what tokens have been read. We often read a token and *then*
                // make a decision about whether to include it in the current span.
                Span.Start = nextStart;
            }
        }

        protected IDisposable PushSpanConfig()
        {
            return PushSpanConfig(newConfig: (Action<SpanBuilder, Action<SpanBuilder>>)null);
        }

        protected IDisposable PushSpanConfig(Action<SpanBuilder> newConfig)
        {
            return PushSpanConfig(newConfig == null ? (Action<SpanBuilder, Action<SpanBuilder>>)null : (span, _) => newConfig(span));
        }

        protected IDisposable PushSpanConfig(Action<SpanBuilder, Action<SpanBuilder>> newConfig)
        {
            var old = SpanConfig;
            ConfigureSpan(newConfig);
            return new DisposableAction(() => SpanConfig = old);
        }

        protected void ConfigureSpan(Action<SpanBuilder> config)
        {
            SpanConfig = config;
            Initialize(Span);
        }

        protected void ConfigureSpan(Action<SpanBuilder, Action<SpanBuilder>> config)
        {
            var prev = SpanConfig;
            if (config == null)
            {
                SpanConfig = null;
            }
            else
            {
                SpanConfig = span => config(span, prev);
            }
            Initialize(Span);
        }

        protected internal void Expected(KnownTokenType type)
        {
            Expected(Language.GetKnownTokenType(type));
        }

        protected internal void Expected(params SyntaxKind[] types)
        {
            Debug.Assert(!EndOfFile && CurrentToken != null && types.Contains(CurrentToken.Kind));
            AcceptAndMoveNext();
        }

        protected internal bool Optional(KnownTokenType type)
        {
            return Optional(Language.GetKnownTokenType(type));
        }

        protected internal bool Optional(SyntaxKind type)
        {
            if (At(type))
            {
                AcceptAndMoveNext();
                return true;
            }
            return false;
        }

        protected bool EnsureCurrent()
        {
            if (CurrentToken == null)
            {
                return NextToken();
            }

            return true;
        }

        protected internal void AcceptWhile(SyntaxKind type)
        {
            AcceptWhile(token => TokenKindEquals(type, token.Kind));
        }

        // We want to avoid array allocations and enumeration where possible, so we use the same technique as string.Format
        protected internal void AcceptWhile(SyntaxKind type1, SyntaxKind type2)
        {
            AcceptWhile(token => TokenKindEquals(type1, token.Kind) || TokenKindEquals(type2, token.Kind));
        }

        protected internal void AcceptWhile(SyntaxKind type1, SyntaxKind type2, SyntaxKind type3)
        {
            AcceptWhile(token => TokenKindEquals(type1, token.Kind) || TokenKindEquals(type2, token.Kind) || TokenKindEquals(type3, token.Kind));
        }

        protected internal void AcceptWhile(params SyntaxKind[] types)
        {
            AcceptWhile(token => types.Any(expected => TokenKindEquals(expected, token.Kind)));
        }

        protected internal void AcceptUntil(SyntaxKind type)
        {
            AcceptWhile(token => !TokenKindEquals(type, token.Kind));
        }

        // We want to avoid array allocations and enumeration where possible, so we use the same technique as string.Format
        protected internal void AcceptUntil(SyntaxKind type1, SyntaxKind type2)
        {
            AcceptWhile(token => !TokenKindEquals(type1, token.Kind) && !TokenKindEquals(type2, token.Kind));
        }

        protected internal void AcceptUntil(SyntaxKind type1, SyntaxKind type2, SyntaxKind type3)
        {
            AcceptWhile(token => !TokenKindEquals(type1, token.Kind) && !TokenKindEquals(type2, token.Kind) && !TokenKindEquals(type3, token.Kind));
        }

        protected internal void AcceptUntil(params SyntaxKind[] types)
        {
            AcceptWhile(token => types.All(expected => !TokenKindEquals(expected, token.Kind)));
        }

        protected internal void AcceptWhile(Func<SyntaxToken, bool> condition)
        {
            Accept(ReadWhileLazy(condition));
        }

        protected internal IEnumerable<SyntaxToken> ReadWhile(Func<SyntaxToken, bool> condition)
        {
            return ReadWhileLazy(condition).ToList();
        }

        protected SyntaxToken AcceptWhiteSpaceInLines()
        {
            SyntaxToken lastWs = null;
            while (Language.IsWhiteSpace(CurrentToken) || Language.IsNewLine(CurrentToken))
            {
                // Capture the previous whitespace node
                if (lastWs != null)
                {
                    Accept(lastWs);
                }

                if (Language.IsWhiteSpace(CurrentToken))
                {
                    lastWs = CurrentToken;
                }
                else if (Language.IsNewLine(CurrentToken))
                {
                    // Accept newline and reset last whitespace tracker
                    Accept(CurrentToken);
                    lastWs = null;
                }

                _tokenizer.Next();
            }
            return lastWs;
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

        private void Configure(SpanKindInternal? kind, AcceptedCharactersInternal? accepts)
        {
            if (kind != null)
            {
                Span.Kind = kind.Value;
            }
            if (accepts != null)
            {
                Span.EditHandler.AcceptedCharacters = accepts.Value;
            }
        }

        protected virtual void OutputSpanBeforeRazorComment()
        {
            throw new InvalidOperationException(Resources.Language_Does_Not_Support_RazorComment);
        }

        private void CommentSpanConfig(SpanBuilder span)
        {
            span.ChunkGenerator = SpanChunkGenerator.Null;
            span.EditHandler = SpanEditHandler.CreateDefault(Language.TokenizeString);
        }

        protected void RazorComment()
        {
            if (!Language.KnowsTokenType(KnownTokenType.CommentStart) ||
                !Language.KnowsTokenType(KnownTokenType.CommentStar) ||
                !Language.KnowsTokenType(KnownTokenType.CommentBody))
            {
                throw new InvalidOperationException(Resources.Language_Does_Not_Support_RazorComment);
            }
            OutputSpanBeforeRazorComment();
            using (PushSpanConfig(CommentSpanConfig))
            {
                using (Context.Builder.StartBlock(BlockKindInternal.Comment))
                {
                    Context.Builder.CurrentBlock.ChunkGenerator = new RazorCommentChunkGenerator();
                    var start = CurrentStart;

                    Expected(KnownTokenType.CommentStart);
                    Output(SpanKindInternal.Transition, AcceptedCharactersInternal.None);

                    Expected(KnownTokenType.CommentStar);
                    Output(SpanKindInternal.MetaCode, AcceptedCharactersInternal.None);

                    Optional(KnownTokenType.CommentBody);
                    AddMarkerTokenIfNecessary();
                    Output(SpanKindInternal.Comment);

                    var errorReported = false;
                    if (!Optional(KnownTokenType.CommentStar))
                    {
                        errorReported = true;
                        Context.ErrorSink.OnError(
                            RazorDiagnosticFactory.CreateParsing_RazorCommentNotTerminated(
                                new SourceSpan(start, contentLength: 2 /* @* */)));
                    }
                    else
                    {
                        Output(SpanKindInternal.MetaCode, AcceptedCharactersInternal.None);
                    }

                    if (!Optional(KnownTokenType.CommentStart))
                    {
                        if (!errorReported)
                        {
                            errorReported = true;
                            Context.ErrorSink.OnError(
                            RazorDiagnosticFactory.CreateParsing_RazorCommentNotTerminated(
                                new SourceSpan(start, contentLength: 2 /* @* */)));
                        }
                    }
                    else
                    {
                        Output(SpanKindInternal.Transition, AcceptedCharactersInternal.None);
                    }
                }
            }
            Initialize(Span);
        }
    }
}
