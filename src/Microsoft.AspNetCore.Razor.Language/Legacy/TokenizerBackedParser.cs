// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language.Syntax.InternalSyntax;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    internal abstract partial class TokenizerBackedParser<TTokenizer, TToken, TTokenType> : ParserBase
        where TTokenType : struct
        where TTokenizer : Tokenizer<TToken, TTokenType>
        where TToken : TokenBase<TTokenType>
    {
        private readonly TokenizerView<TTokenizer, TToken, TTokenType> _tokenizer;

        protected TokenizerBackedParser(LanguageCharacteristics<TTokenizer, TToken, TTokenType> language, ParserContext context)
            : base(context)
        {
            Language = language;

            var languageTokenizer = Language.CreateTokenizer(Context.Source);
            _tokenizer = new TokenizerView<TTokenizer, TToken, TTokenType>(languageTokenizer);
            Span = new SpanBuilder(CurrentLocation);
        }

        protected ParserState ParserState { get; set; }

        protected SpanBuilder Span { get; private set; }

        protected Action<SpanBuilder> SpanConfig { get; set; }

        protected TToken CurrentToken
        {
            get { return _tokenizer.Current; }
        }

        protected SyntaxToken CurrentSyntaxToken => CurrentToken?.SyntaxToken;

        protected TToken PreviousToken { get; private set; }

        protected SourceLocation CurrentLocation => _tokenizer.Tokenizer.CurrentLocation;

        protected SourceLocation CurrentStart => _tokenizer.Tokenizer.CurrentStart;

        protected bool EndOfFile
        {
            get { return _tokenizer.EndOfFile; }
        }

        protected LanguageCharacteristics<TTokenizer, TToken, TTokenType> Language { get; }

        protected virtual void HandleEmbeddedTransition()
        {
        }

        protected virtual bool IsAtEmbeddedTransition(bool allowTemplatesAndComments, bool allowTransitions)
        {
            return false;
        }

        public override void BuildSpan(SpanBuilder span, SourceLocation start, string content)
        {
            foreach (IToken sym in Language.TokenizeString(start, content))
            {
                span.Accept(sym);
            }
        }

        protected void Initialize(SpanBuilder span)
        {
            if (SpanConfig != null)
            {
                SpanConfig(span);
            }
        }

        protected TToken Lookahead(int count)
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
            var tokens = new TToken[count + 1];
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
        protected bool LookaheadUntil(Func<TToken, IEnumerable<TToken>, bool> condition)
        {
            if (condition == null)
            {
                throw new ArgumentNullException(nameof(condition));
            }

            var matchFound = false;

            var tokens = new List<TToken>();
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
        internal void Assert(TTokenType expectedType)
        {
            Debug.Assert(!EndOfFile && TokenTypeEquals(CurrentToken.Type, expectedType));
        }

        abstract protected bool TokenTypeEquals(TTokenType x, TTokenType y);

        protected internal void PutBack(TToken token)
        {
            if (token != null)
            {
                _tokenizer.PutBack(token);
            }
        }

        /// <summary>
        /// Put the specified tokens back in the input stream. The provided list MUST be in the ORDER THE TOKENS WERE READ. The
        /// list WILL be reversed and the Putback(TToken) will be called on each item.
        /// </summary>
        /// <remarks>
        /// If a document contains tokens: a, b, c, d, e, f
        /// and AcceptWhile or AcceptUntil is used to collect until d
        /// the list returned by AcceptWhile/Until will contain: a, b, c IN THAT ORDER
        /// that is the correct format for providing to this method. The caller of this method would,
        /// in that case, want to put c, b and a back into the stream, so "a, b, c" is the CORRECT order
        /// </remarks>
        protected internal void PutBack(IEnumerable<TToken> tokens)
        {
            foreach (TToken token in tokens.Reverse())
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
            var left = CurrentToken.Type;
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

        protected internal bool Balance(BalancingModes mode, TTokenType left, TTokenType right, SourceLocation start)
        {
            var startPosition = CurrentStart.AbsoluteIndex;
            var nesting = 1;
            if (!EndOfFile)
            {
                var syms = new List<TToken>();
                do
                {
                    if (IsAtEmbeddedTransition(
                        (mode & BalancingModes.AllowCommentsAndTemplates) == BalancingModes.AllowCommentsAndTemplates,
                        (mode & BalancingModes.AllowEmbeddedTransitions) == BalancingModes.AllowEmbeddedTransitions))
                    {
                        Accept(syms);
                        syms.Clear();
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
                        syms.Add(CurrentToken);
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
                        Accept(syms);
                    }
                }
                else
                {
                    // Accept all the tokens we saw
                    Accept(syms);
                }
            }
            return nesting == 0;
        }

        protected internal bool NextIs(TTokenType type)
        {
            return NextIs(sym => sym != null && TokenTypeEquals(type, sym.Type));
        }

        protected internal bool NextIs(params TTokenType[] types)
        {
            return NextIs(sym => sym != null && types.Any(t => TokenTypeEquals(t, sym.Type)));
        }

        protected internal bool NextIs(Func<TToken, bool> condition)
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

        protected internal bool Was(TTokenType type)
        {
            return PreviousToken != null && TokenTypeEquals(PreviousToken.Type, type);
        }

        protected internal bool At(TTokenType type)
        {
            return !EndOfFile && CurrentToken != null && TokenTypeEquals(CurrentToken.Type, type);
        }

        protected internal bool AcceptAndMoveNext()
        {
            Accept(CurrentToken);
            return NextToken();
        }

        protected TToken AcceptSingleWhiteSpaceCharacter()
        {
            if (Language.IsWhiteSpace(CurrentToken))
            {
                Tuple<TToken, TToken> pair = Language.SplitToken(CurrentToken, 1, Language.GetKnownTokenType(KnownTokenType.WhiteSpace));
                Accept(pair.Item1);
                Span.EditHandler.AcceptedCharacters = AcceptedCharactersInternal.None;
                NextToken();
                return pair.Item2;
            }
            return null;
        }

        protected internal void Accept(IEnumerable<TToken> tokens)
        {
            foreach (TToken token in tokens)
            {
                Accept(token);
            }
        }

        protected internal void Accept(TToken token)
        {
            if (token != null)
            {
                foreach (var error in token.Errors)
                {
                    Context.ErrorSink.OnError(error);
                }

                Span.Accept(token);
            }
        }

        protected internal bool AcceptAll(params TTokenType[] types)
        {
            foreach (TTokenType type in types)
            {
                if (CurrentToken == null || !TokenTypeEquals(CurrentToken.Type, type))
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
            Action<SpanBuilder> old = SpanConfig;
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
            Action<SpanBuilder> prev = SpanConfig;
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

        protected internal void Expected(params TTokenType[] types)
        {
            Debug.Assert(!EndOfFile && CurrentToken != null && types.Contains(CurrentToken.Type));
            AcceptAndMoveNext();
        }

        protected internal bool Optional(KnownTokenType type)
        {
            return Optional(Language.GetKnownTokenType(type));
        }

        protected internal bool Optional(TTokenType type)
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

        protected internal void AcceptWhile(TTokenType type)
        {
            AcceptWhile(sym => TokenTypeEquals(type, sym.Type));
        }

        // We want to avoid array allocations and enumeration where possible, so we use the same technique as string.Format
        protected internal void AcceptWhile(TTokenType type1, TTokenType type2)
        {
            AcceptWhile(sym => TokenTypeEquals(type1, sym.Type) || TokenTypeEquals(type2, sym.Type));
        }

        protected internal void AcceptWhile(TTokenType type1, TTokenType type2, TTokenType type3)
        {
            AcceptWhile(sym => TokenTypeEquals(type1, sym.Type) || TokenTypeEquals(type2, sym.Type) || TokenTypeEquals(type3, sym.Type));
        }

        protected internal void AcceptWhile(params TTokenType[] types)
        {
            AcceptWhile(sym => types.Any(expected => TokenTypeEquals(expected, sym.Type)));
        }

        protected internal void AcceptUntil(TTokenType type)
        {
            AcceptWhile(sym => !TokenTypeEquals(type, sym.Type));
        }

        // We want to avoid array allocations and enumeration where possible, so we use the same technique as string.Format
        protected internal void AcceptUntil(TTokenType type1, TTokenType type2)
        {
            AcceptWhile(sym => !TokenTypeEquals(type1, sym.Type) && !TokenTypeEquals(type2, sym.Type));
        }

        protected internal void AcceptUntil(TTokenType type1, TTokenType type2, TTokenType type3)
        {
            AcceptWhile(sym => !TokenTypeEquals(type1, sym.Type) && !TokenTypeEquals(type2, sym.Type) && !TokenTypeEquals(type3, sym.Type));
        }

        protected internal void AcceptUntil(params TTokenType[] types)
        {
            AcceptWhile(sym => types.All(expected => !TokenTypeEquals(expected, sym.Type)));
        }

        protected internal void AcceptWhile(Func<TToken, bool> condition)
        {
            Accept(ReadWhileLazy(condition));
        }

        protected internal IEnumerable<TToken> ReadWhile(Func<TToken, bool> condition)
        {
            return ReadWhileLazy(condition).ToList();
        }

        protected TToken AcceptWhiteSpaceInLines()
        {
            TToken lastWs = null;
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
        internal IEnumerable<TToken> ReadWhileLazy(Func<TToken, bool> condition)
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
