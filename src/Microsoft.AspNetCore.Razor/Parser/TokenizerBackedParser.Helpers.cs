// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Razor.Chunks.Generators;
using Microsoft.AspNetCore.Razor.Editor;
using Microsoft.AspNetCore.Razor.Parser.Internal;
using Microsoft.AspNetCore.Razor.Parser.SyntaxTree;
using Microsoft.AspNetCore.Razor.Tokenizer;
using Microsoft.AspNetCore.Razor.Tokenizer.Symbols;

namespace Microsoft.AspNetCore.Razor.Parser
{
    public abstract partial class TokenizerBackedParser<TTokenizer, TSymbol, TSymbolType> : ParserBase
        where TSymbolType : struct
        where TTokenizer : Tokenizer<TSymbol, TSymbolType>
        where TSymbol : SymbolBase<TSymbolType>
    {
        // Helpers
        [Conditional("DEBUG")]
        internal void Assert(TSymbolType expectedType)
        {
            Debug.Assert(!EndOfFile && SymbolTypeEquals(CurrentSymbol.Type, expectedType));
        }

        abstract protected bool SymbolTypeEquals(TSymbolType x, TSymbolType y);

        protected internal void PutBack(TSymbol symbol)
        {
            if (symbol != null)
            {
                Tokenizer.PutBack(symbol);
            }
        }

        /// <summary>
        /// Put the specified symbols back in the input stream. The provided list MUST be in the ORDER THE SYMBOLS WERE READ. The
        /// list WILL be reversed and the Putback(TSymbol) will be called on each item.
        /// </summary>
        /// <remarks>
        /// If a document contains symbols: a, b, c, d, e, f
        /// and AcceptWhile or AcceptUntil is used to collect until d
        /// the list returned by AcceptWhile/Until will contain: a, b, c IN THAT ORDER
        /// that is the correct format for providing to this method. The caller of this method would,
        /// in that case, want to put c, b and a back into the stream, so "a, b, c" is the CORRECT order
        /// </remarks>
        protected internal void PutBack(IEnumerable<TSymbol> symbols)
        {
            foreach (TSymbol symbol in symbols.Reverse())
            {
                PutBack(symbol);
            }
        }

        protected internal void PutCurrentBack()
        {
            if (!EndOfFile && CurrentSymbol != null)
            {
                PutBack(CurrentSymbol);
            }
        }

        protected internal bool Balance(BalancingModes mode)
        {
            var left = CurrentSymbol.Type;
            var right = Language.FlipBracket(left);
            var start = CurrentLocation;
            AcceptAndMoveNext();
            if (EndOfFile && ((mode & BalancingModes.NoErrorOnFailure) != BalancingModes.NoErrorOnFailure))
            {
                Context.OnError(
                    start,
                    RazorResources.FormatParseError_Expected_CloseBracket_Before_EOF(
                        Language.GetSample(left),
                        Language.GetSample(right)),
                    length: 1 /* { OR } */);
            }

            return Balance(mode, left, right, start);
        }

        protected internal bool Balance(BalancingModes mode, TSymbolType left, TSymbolType right, SourceLocation start)
        {
            var startPosition = CurrentLocation.AbsoluteIndex;
            var nesting = 1;
            if (!EndOfFile)
            {
                IList<TSymbol> syms = new List<TSymbol>();
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
                        startPosition = CurrentLocation.AbsoluteIndex;
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
                        syms.Add(CurrentSymbol);
                    }
                }
                while (nesting > 0 && NextToken());

                if (nesting > 0)
                {
                    if ((mode & BalancingModes.NoErrorOnFailure) != BalancingModes.NoErrorOnFailure)
                    {
                        Context.OnError(
                            start,
                            RazorResources.FormatParseError_Expected_CloseBracket_Before_EOF(
                                Language.GetSample(left),
                                Language.GetSample(right)),
                            length: 1 /* { OR } */);
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
                    // Accept all the symbols we saw
                    Accept(syms);
                }
            }
            return nesting == 0;
        }

        protected internal bool NextIs(TSymbolType type)
        {
            return NextIs(sym => sym != null && SymbolTypeEquals(type, sym.Type));
        }

        protected internal bool NextIs(params TSymbolType[] types)
        {
            return NextIs(sym => sym != null && types.Any(t => SymbolTypeEquals(t, sym.Type)));
        }

        protected internal bool NextIs(Func<TSymbol, bool> condition)
        {
            var cur = CurrentSymbol;
            NextToken();
            var result = condition(CurrentSymbol);
            PutCurrentBack();
            PutBack(cur);
            EnsureCurrent();
            return result;
        }

        protected internal bool Was(TSymbolType type)
        {
            return PreviousSymbol != null && SymbolTypeEquals(PreviousSymbol.Type, type);
        }

        protected internal bool At(TSymbolType type)
        {
            return !EndOfFile && CurrentSymbol != null && SymbolTypeEquals(CurrentSymbol.Type, type);
        }

        protected internal bool AcceptAndMoveNext()
        {
            Accept(CurrentSymbol);
            return NextToken();
        }

        protected TSymbol AcceptSingleWhiteSpaceCharacter()
        {
            if (Language.IsWhiteSpace(CurrentSymbol))
            {
                Tuple<TSymbol, TSymbol> pair = Language.SplitSymbol(CurrentSymbol, 1, Language.GetKnownSymbolType(KnownSymbolType.WhiteSpace));
                Accept(pair.Item1);
                Span.EditHandler.AcceptedCharacters = AcceptedCharacters.None;
                NextToken();
                return pair.Item2;
            }
            return null;
        }

        protected internal void Accept(IEnumerable<TSymbol> symbols)
        {
            foreach (TSymbol symbol in symbols)
            {
                Accept(symbol);
            }
        }

        protected internal void Accept(TSymbol symbol)
        {
            if (symbol != null)
            {
                foreach (RazorError error in symbol.Errors)
                {
                    Context.OnError(error);
                }
                Span.Accept(symbol);
            }
        }

        protected internal bool AcceptAll(params TSymbolType[] types)
        {
            foreach (TSymbolType type in types)
            {
                if (CurrentSymbol == null || !SymbolTypeEquals(CurrentSymbol.Type, type))
                {
                    return false;
                }
                AcceptAndMoveNext();
            }
            return true;
        }

        protected internal void AddMarkerSymbolIfNecessary()
        {
            AddMarkerSymbolIfNecessary(CurrentLocation);
        }

        protected internal void AddMarkerSymbolIfNecessary(SourceLocation location)
        {
            if (Span.Symbols.Count == 0 && Context.LastAcceptedCharacters != AcceptedCharacters.Any)
            {
                Accept(Language.CreateMarkerSymbol(location));
            }
        }

        protected internal void Output(SpanKind kind)
        {
            Configure(kind, null);
            Output();
        }

        protected internal void Output(SpanKind kind, AcceptedCharacters accepts)
        {
            Configure(kind, accepts);
            Output();
        }

        protected internal void Output(AcceptedCharacters accepts)
        {
            Configure(null, accepts);
            Output();
        }

        private void Output()
        {
            if (Span.Symbols.Count > 0)
            {
                Context.AddSpan(Span.Build());
                Initialize(Span);
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

        protected internal void Expected(KnownSymbolType type)
        {
            Expected(Language.GetKnownSymbolType(type));
        }

        protected internal void Expected(params TSymbolType[] types)
        {
            Debug.Assert(!EndOfFile && CurrentSymbol != null && types.Contains(CurrentSymbol.Type));
            AcceptAndMoveNext();
        }

        protected internal bool Optional(KnownSymbolType type)
        {
            return Optional(Language.GetKnownSymbolType(type));
        }

        protected internal bool Optional(TSymbolType type)
        {
            if (At(type))
            {
                AcceptAndMoveNext();
                return true;
            }
            return false;
        }

        protected internal bool Required(TSymbolType expected, bool errorIfNotFound, Func<string, string> errorBase)
        {
            var found = At(expected);
            if (!found && errorIfNotFound)
            {
                string error;
                if (Language.IsNewLine(CurrentSymbol))
                {
                    error = RazorResources.ErrorComponent_Newline;
                }
                else if (Language.IsWhiteSpace(CurrentSymbol))
                {
                    error = RazorResources.ErrorComponent_Whitespace;
                }
                else if (EndOfFile)
                {
                    error = RazorResources.ErrorComponent_EndOfFile;
                }
                else
                {
                    error = RazorResources.FormatErrorComponent_Character(CurrentSymbol.Content);
                }

                int errorLength;
                if (CurrentSymbol == null || CurrentSymbol.Content == null)
                {
                    errorLength = 1;
                }
                else
                {
                    errorLength = Math.Max(CurrentSymbol.Content.Length, 1);
                }

                Context.OnError(CurrentLocation, errorBase(error), errorLength);
            }
            return found;
        }

        protected bool EnsureCurrent()
        {
            if (CurrentSymbol == null)
            {
                return NextToken();
            }
            return true;
        }

        protected internal void AcceptWhile(TSymbolType type)
        {
            AcceptWhile(sym => SymbolTypeEquals(type, sym.Type));
        }

        // We want to avoid array allocations and enumeration where possible, so we use the same technique as string.Format
        protected internal void AcceptWhile(TSymbolType type1, TSymbolType type2)
        {
            AcceptWhile(sym => SymbolTypeEquals(type1, sym.Type) || SymbolTypeEquals(type2, sym.Type));
        }

        protected internal void AcceptWhile(TSymbolType type1, TSymbolType type2, TSymbolType type3)
        {
            AcceptWhile(sym => SymbolTypeEquals(type1, sym.Type) || SymbolTypeEquals(type2, sym.Type) || SymbolTypeEquals(type3, sym.Type));
        }

        protected internal void AcceptWhile(params TSymbolType[] types)
        {
            AcceptWhile(sym => types.Any(expected => SymbolTypeEquals(expected, sym.Type)));
        }

        protected internal void AcceptUntil(TSymbolType type)
        {
            AcceptWhile(sym => !SymbolTypeEquals(type, sym.Type));
        }

        // We want to avoid array allocations and enumeration where possible, so we use the same technique as string.Format
        protected internal void AcceptUntil(TSymbolType type1, TSymbolType type2)
        {
            AcceptWhile(sym => !SymbolTypeEquals(type1, sym.Type) && !SymbolTypeEquals(type2, sym.Type));
        }

        protected internal void AcceptUntil(TSymbolType type1, TSymbolType type2, TSymbolType type3)
        {
            AcceptWhile(sym => !SymbolTypeEquals(type1, sym.Type) && !SymbolTypeEquals(type2, sym.Type) && !SymbolTypeEquals(type3, sym.Type));
        }

        protected internal void AcceptUntil(params TSymbolType[] types)
        {
            AcceptWhile(sym => types.All(expected => !SymbolTypeEquals(expected, sym.Type)));
        }

        protected internal void AcceptWhile(Func<TSymbol, bool> condition)
        {
            Accept(ReadWhileLazy(condition));
        }

        protected internal IEnumerable<TSymbol> ReadWhile(Func<TSymbol, bool> condition)
        {
            return ReadWhileLazy(condition).ToList();
        }

        protected TSymbol AcceptWhiteSpaceInLines()
        {
            TSymbol lastWs = null;
            while (Language.IsWhiteSpace(CurrentSymbol) || Language.IsNewLine(CurrentSymbol))
            {
                // Capture the previous whitespace node
                if (lastWs != null)
                {
                    Accept(lastWs);
                }

                if (Language.IsWhiteSpace(CurrentSymbol))
                {
                    lastWs = CurrentSymbol;
                }
                else if (Language.IsNewLine(CurrentSymbol))
                {
                    // Accept newline and reset last whitespace tracker
                    Accept(CurrentSymbol);
                    lastWs = null;
                }

                Tokenizer.Next();
            }
            return lastWs;
        }

        protected bool AtIdentifier(bool allowKeywords)
        {
            return CurrentSymbol != null &&
                   (Language.IsIdentifier(CurrentSymbol) ||
                    (allowKeywords && Language.IsKeyword(CurrentSymbol)));
        }

        // Don't open this to sub classes because it's lazy but it looks eager.
        // You have to advance the Enumerable to read the next characters.
        internal IEnumerable<TSymbol> ReadWhileLazy(Func<TSymbol, bool> condition)
        {
            while (EnsureCurrent() && condition(CurrentSymbol))
            {
                yield return CurrentSymbol;
                NextToken();
            }
        }

        private void Configure(SpanKind? kind, AcceptedCharacters? accepts)
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
            throw new InvalidOperationException(RazorResources.Language_Does_Not_Support_RazorComment);
        }

        private void CommentSpanConfig(SpanBuilder span)
        {
            span.ChunkGenerator = SpanChunkGenerator.Null;
            span.EditHandler = SpanEditHandler.CreateDefault(Language.TokenizeString);
        }

        protected void RazorComment()
        {
            if (!Language.KnowsSymbolType(KnownSymbolType.CommentStart) ||
                !Language.KnowsSymbolType(KnownSymbolType.CommentStar) ||
                !Language.KnowsSymbolType(KnownSymbolType.CommentBody))
            {
                throw new InvalidOperationException(RazorResources.Language_Does_Not_Support_RazorComment);
            }
            OutputSpanBeforeRazorComment();
            using (PushSpanConfig(CommentSpanConfig))
            {
                using (Context.StartBlock(BlockType.Comment))
                {
                    Context.CurrentBlock.ChunkGenerator = new RazorCommentChunkGenerator();
                    var start = CurrentLocation;

                    Expected(KnownSymbolType.CommentStart);
                    Output(SpanKind.Transition, AcceptedCharacters.None);

                    Expected(KnownSymbolType.CommentStar);
                    Output(SpanKind.MetaCode, AcceptedCharacters.None);

                    Optional(KnownSymbolType.CommentBody);
                    AddMarkerSymbolIfNecessary();
                    Output(SpanKind.Comment);

                    var errorReported = false;
                    if (!Optional(KnownSymbolType.CommentStar))
                    {
                        errorReported = true;
                        Context.OnError(
                            start,
                            RazorResources.ParseError_RazorComment_Not_Terminated,
                            length: 2 /* @* */);
                    }
                    else
                    {
                        Output(SpanKind.MetaCode, AcceptedCharacters.None);
                    }

                    if (!Optional(KnownSymbolType.CommentStart))
                    {
                        if (!errorReported)
                        {
                            errorReported = true;
                            Context.OnError(
                                start,
                                RazorResources.ParseError_RazorComment_Not_Terminated,
                                length: 2 /* @* */);
                        }
                    }
                    else
                    {
                        Output(SpanKind.Transition, AcceptedCharacters.None);
                    }
                }
            }
            Initialize(Span);
        }
    }
}
