// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
#if DEBUG
using System.Globalization;
#endif
using System.Text;
using Microsoft.AspNet.Razor.Text;
using Microsoft.AspNet.Razor.Tokenizer.Symbols;

namespace Microsoft.AspNet.Razor.Tokenizer
{
    [DebuggerDisplay("{DebugDisplay}")]
    public abstract partial class Tokenizer<TSymbol, TSymbolType> : ITokenizer
        where TSymbolType : struct
        where TSymbol : SymbolBase<TSymbolType>
    {
#if DEBUG
        private StringBuilder _read = new StringBuilder();
#endif

        protected Tokenizer(ITextDocument source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            Source = new TextDocumentReader(source);
            Buffer = new StringBuilder();
            CurrentErrors = new List<RazorError>();
            StartSymbol();
        }

        protected abstract int StartState { get; }

        protected int? CurrentState { get; set; }

        protected TSymbol CurrentSymbol { get; private set; }

        public TextDocumentReader Source { get; private set; }

        protected StringBuilder Buffer { get; private set; }

        protected bool EndOfFile
        {
            get { return Source.Peek() == -1; }
        }

        protected IList<RazorError> CurrentErrors { get; private set; }

        public abstract TSymbolType RazorCommentStarType { get; }
        public abstract TSymbolType RazorCommentType { get; }
        public abstract TSymbolType RazorCommentTransitionType { get; }

        protected bool HaveContent
        {
            get { return Buffer.Length > 0; }
        }

        protected char CurrentCharacter
        {
            get
            {
                var peek = Source.Peek();
                return peek == -1 ? '\0' : (char)peek;
            }
        }

        protected SourceLocation CurrentLocation
        {
            get { return Source.Location; }
        }

        protected SourceLocation CurrentStart { get; private set; }

        protected abstract TSymbol CreateSymbol(SourceLocation start, string content, TSymbolType type, IReadOnlyList<RazorError> errors);

        protected abstract StateResult Dispatch();

        public virtual TSymbol NextSymbol()
        {
            // Post-Condition: Buffer should be empty at the start of Next()
            Debug.Assert(Buffer.Length == 0);
            StartSymbol();

            if (EndOfFile)
            {
                return null;
            }

            var symbol = Turn();

            // Post-Condition: Buffer should be empty at the end of Next()
            Debug.Assert(Buffer.Length == 0);

            return symbol;
        }

        protected virtual TSymbol Turn()
        {
            if (CurrentState != null)
            {
                // Run until we get into the stop state or have a result.
                do
                {
                    var next = Dispatch();

                    CurrentState = next.State;
                    CurrentSymbol = next.Result;
                }
                while (CurrentState != null && CurrentSymbol == null); 

                if (CurrentState == null)
                {
                    return default(TSymbol); // Terminated
                }

                return CurrentSymbol;
            }

            return default(TSymbol);
        }

        public void Reset()
        {
            CurrentState = StartState;
        }

        /// <summary>
        /// Returns a result indicating that the machine should stop executing and return null output.
        /// </summary>
        protected StateResult Stop()
        {
            return default(StateResult);
        }

        /// <summary>
        /// Returns a result indicating that this state has no output and the machine should immediately invoke the specified state
        /// </summary>
        /// <remarks>
        /// By returning no output, the state machine will invoke the next state immediately, before returning
        /// controller to the caller of <see cref="Turn"/>
        /// </remarks>
        protected StateResult Transition(int state)
        {
            return new StateResult(state, result: null);
        }

        /// <summary>
        /// Returns a result containing the specified output and indicating that the next call to
        /// <see cref="Turn"/> should invoke the provided state.
        /// </summary>
        protected StateResult Transition(int state, TSymbol result)
        {
            return new StateResult(state, result);
        }

        protected StateResult Transition(RazorCommentTokenizerState state)
        {
            return new StateResult((int)state, result: null);
        }

        protected StateResult Transition(RazorCommentTokenizerState state, TSymbol result)
        {
            return new StateResult((int)state, result);
        }

        /// <summary>
        /// Returns a result indicating that this state has no output and the machine should remain in this state
        /// </summary>
        /// <remarks>
        /// By returning no output, the state machine will re-invoke the current state again before returning
        /// controller to the caller of <see cref="Turn"/>
        /// </remarks>
        protected StateResult Stay()
        {
            return new StateResult(CurrentState, result: null);
        }

        /// <summary>
        /// Returns a result containing the specified output and indicating that the next call to
        /// <see cref="Turn"/> should re-invoke the current state.
        /// </summary>
        protected StateResult Stay(TSymbol result)
        {
            return new StateResult(CurrentState, result);
        }

        protected TSymbol Single(TSymbolType type)
        {
            TakeCurrent();
            return EndSymbol(type);
        }

        protected void StartSymbol()
        {
            Buffer.Clear();
            CurrentStart = CurrentLocation;
            CurrentErrors.Clear();
        }

        protected TSymbol EndSymbol(TSymbolType type)
        {
            return EndSymbol(CurrentStart, type);
        }

        protected TSymbol EndSymbol(SourceLocation start, TSymbolType type)
        {
            TSymbol sym = null;
            if (HaveContent)
            {
                // Perf: Don't allocate a new errors array unless necessary.
                var errors = CurrentErrors.Count == 0 ? RazorError.EmptyArray : new RazorError[CurrentErrors.Count];
                for (var i = 0; i < CurrentErrors.Count; i++)
                {
                    errors[i] = CurrentErrors[i];
                }

                sym = CreateSymbol(start, Buffer.ToString(), type, errors);
            }
            StartSymbol();
            return sym;
        }

        protected bool TakeUntil(Func<char, bool> predicate)
        {
            // Take all the characters up to the end character
            while (!EndOfFile && !predicate(CurrentCharacter))
            {
                TakeCurrent();
            }

            // Why did we end?
            return !EndOfFile;
        }

        protected void TakeCurrent()
        {
            if (EndOfFile)
            {
                return;
            } // No-op
            Buffer.Append(CurrentCharacter);
            MoveNext();
        }

        protected void MoveNext()
        {
#if DEBUG
            _read.Append(CurrentCharacter);
#endif
            Source.Read();
        }

        protected bool TakeAll(string expected, bool caseSensitive)
        {
            return Lookahead(expected, takeIfMatch: true, caseSensitive: caseSensitive);
        }

        protected char Peek()
        {
            using (var lookahead = Source.BeginLookahead())
            {
                MoveNext();
                return CurrentCharacter;
            }
        }

        protected StateResult AfterRazorCommentTransition()
        {
            if (CurrentCharacter != '*')
            {
                // We've been moved since last time we were asked for a symbol... reset the state
                return Transition(StartState);
            }

            AssertCurrent('*');
            TakeCurrent();
            return Transition(1002, EndSymbol(RazorCommentStarType));
        }

        protected StateResult RazorCommentBody()
        {
            TakeUntil(c => c == '*');
            if (CurrentCharacter == '*')
            {
                if (Peek() == '@')
                {
                    if (HaveContent)
                    {
                        return Transition(
                            RazorCommentTokenizerState.StarAfterRazorCommentBody,
                            EndSymbol(RazorCommentType));
                    }
                    else
                    {
                        return Transition(RazorCommentTokenizerState.StarAfterRazorCommentBody);
                    }
                }
                else
                {
                    TakeCurrent();
                    return Stay();
                }
            }

            return Transition(StartState, EndSymbol(RazorCommentType));
        }

        protected StateResult StarAfterRazorCommentBody()
        {
            AssertCurrent('*');
            TakeCurrent();
            return Transition(
                RazorCommentTokenizerState.AtSymbolAfterRazorCommentBody,
                EndSymbol(RazorCommentStarType));
        }

        protected StateResult AtSymbolAfterRazorCommentBody()
        {
            AssertCurrent('@');
            TakeCurrent();
            return Transition(StartState, EndSymbol(RazorCommentTransitionType));
        }

        /// <summary>
        /// Internal for unit testing
        /// </summary>
        internal bool Lookahead(string expected, bool takeIfMatch, bool caseSensitive)
        {
            Func<char, char> filter = c => c;
            if (!caseSensitive)
            {
                filter = char.ToLowerInvariant;
            }

            if (expected.Length == 0 || filter(CurrentCharacter) != filter(expected[0]))
            {
                return false;
            }

            // Capture the current buffer content in case we have to backtrack
            string oldBuffer = null;
            if (takeIfMatch)
            {
                oldBuffer = Buffer.ToString();
            }

            using (var lookahead = Source.BeginLookahead())
            {
                for (int i = 0; i < expected.Length; i++)
                {
                    if (filter(CurrentCharacter) != filter(expected[i]))
                    {
                        if (takeIfMatch)
                        {
                            // Clear the buffer and put the old buffer text back
                            Buffer.Clear();
                            Buffer.Append(oldBuffer);
                        }
                        // Return without accepting lookahead (thus rejecting it)
                        return false;
                    }
                    if (takeIfMatch)
                    {
                        TakeCurrent();
                    }
                    else
                    {
                        MoveNext();
                    }
                }
                if (takeIfMatch)
                {
                    lookahead.Accept();
                }
            }
            return true;
        }

        [Conditional("DEBUG")]
        internal void AssertCurrent(char current)
        {
#if NET451
            // No Debug.Assert with this many arguments in CoreCLR

            Debug.Assert(CurrentCharacter == current, "CurrentCharacter Assumption violated", "Assumed that the current character would be {0}, but it is actually {1}", current, CurrentCharacter);
#else
            Debug.Assert(CurrentCharacter == current, string.Format("CurrentCharacter Assumption violated.  Assumed that the current character would be {0}, but it is actually {1}", current, CurrentCharacter));
#endif
        }

        ISymbol ITokenizer.NextSymbol()
        {
            return (ISymbol)NextSymbol();
        }

#if DEBUG
        public string DebugDisplay
        {
            get { return string.Format(CultureInfo.InvariantCulture, "[{0}] [{1}] [{2}]", _read.ToString(), CurrentCharacter, Remaining); }
        }

        public string Remaining
        {
            get
            {
                var remaining = Source.ReadToEnd();
                Source.Seek(-remaining.Length);
                return remaining;
            }
        }
#endif

        protected enum RazorCommentTokenizerState
        {
            AfterRazorCommentTransition = 1000,
            EscapedRazorCommentTransition,
            RazorCommentBody,
            StarAfterRazorCommentBody,
            AtSymbolAfterRazorCommentBody,
        }

        protected struct StateResult
        {
            public StateResult(int? state, TSymbol result)
            {
                State = state;
                Result = result;
            }

            public int? State { get; }

            public TSymbol Result { get; }
        }
    }
}
