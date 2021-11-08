// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Microsoft.AspNetCore.Razor.Language.Syntax.InternalSyntax;

namespace Microsoft.AspNetCore.Razor.Language.Legacy;

internal abstract class Tokenizer : ITokenizer
{
    protected Tokenizer(ITextDocument source)
    {
        if (source == null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        Source = source;
        Buffer = new StringBuilder();
        CurrentErrors = new List<RazorDiagnostic>();
        StartToken();
    }

    protected List<RazorDiagnostic> CurrentErrors { get; }

    protected abstract int StartState { get; }

    protected int? CurrentState { get; set; }

    protected SyntaxToken CurrenSyntaxToken { get; private set; }

    public ITextDocument Source { get; private set; }

    protected StringBuilder Buffer { get; private set; }

    protected bool EndOfFile
    {
        get { return Source.Peek() == -1; }
    }

    public abstract SyntaxKind RazorCommentStarKind { get; }
    public abstract SyntaxKind RazorCommentKind { get; }
    public abstract SyntaxKind RazorCommentTransitionKind { get; }

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

    public SourceLocation CurrentLocation => Source.Location;

    public SourceLocation CurrentStart { get; private set; }

    protected abstract SyntaxToken CreateToken(string content, SyntaxKind type, RazorDiagnostic[] errors);

    protected abstract StateResult Dispatch();

    SyntaxToken ITokenizer.NextToken()
    {
        return NextToken();
    }

    public virtual SyntaxToken NextToken()
    {
        // Post-Condition: Buffer should be empty at the start of Next()
        Debug.Assert(Buffer.Length == 0);
        StartToken();

        if (EndOfFile)
        {
            return null;
        }

        var token = Turn();

        // Post-Condition: Buffer should be empty at the end of Next()
        Debug.Assert(Buffer.Length == 0);

        // Post-Condition: Token should be non-zero length unless we're at EOF.
        Debug.Assert(EndOfFile || !CurrentStart.Equals(CurrentLocation));

        return token;
    }

    protected virtual SyntaxToken Turn()
    {
        if (CurrentState != null)
        {
            // Run until we get into the stop state or have a result.
            do
            {
                var next = Dispatch();

                CurrentState = next.State;
                CurrenSyntaxToken = next.Result;
            }
            while (CurrentState != null && CurrenSyntaxToken == null);

            if (CurrentState == null)
            {
                return default(SyntaxToken); // Terminated
            }

            return CurrenSyntaxToken;
        }

        return default(SyntaxToken);
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
    protected StateResult Transition(int state, SyntaxToken result)
    {
        return new StateResult(state, result);
    }

    protected StateResult Transition(RazorCommentTokenizerState state)
    {
        return new StateResult((int)state, result: null);
    }

    protected StateResult Transition(RazorCommentTokenizerState state, SyntaxToken result)
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
    protected StateResult Stay(SyntaxToken result)
    {
        return new StateResult(CurrentState, result);
    }

    protected SyntaxToken Single(SyntaxKind type)
    {
        TakeCurrent();
        return EndToken(type);
    }

    protected void StartToken()
    {
        Debug.Assert(Buffer.Length == 0);
        Debug.Assert(CurrentErrors.Count == 0);

        CurrentStart = CurrentLocation;
    }

    protected SyntaxToken EndToken(SyntaxKind type)
    {
        SyntaxToken token = null;
        if (HaveContent)
        {
            // Perf: Don't allocate a new errors array unless necessary.
            var errors = CurrentErrors.Count == 0 ? RazorDiagnostic.EmptyArray : new RazorDiagnostic[CurrentErrors.Count];
            for (var i = 0; i < CurrentErrors.Count; i++)
            {
                errors[i] = CurrentErrors[i];
            }

            var tokenContent = GetTokenContent(type);
            Debug.Assert(string.Equals(tokenContent, Buffer.ToString(), StringComparison.Ordinal));
            token = CreateToken(tokenContent, type, errors);

            Buffer.Clear();
            CurrentErrors.Clear();
        }

        return token;
    }

    protected virtual string GetTokenContent(SyntaxKind type)
    {
        return Buffer.ToString();
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
        Source.Read();
    }

    protected bool TakeAll(string expected, bool caseSensitive)
    {
        return Lookahead(expected, takeIfMatch: true, caseSensitive: caseSensitive);
    }

    protected char Peek()
    {
        using (var lookahead = BeginLookahead(Source))
        {
            MoveNext();
            return CurrentCharacter;
        }
    }

    protected StateResult AfterRazorCommentTransition()
    {
        if (CurrentCharacter != '*')
        {
            // We've been moved since last time we were asked for a token... reset the state
            return Transition(StartState);
        }

        AssertCurrent('*');
        TakeCurrent();
        return Transition(1002, EndToken(RazorCommentStarKind));
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
                        EndToken(RazorCommentKind));
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

        return Transition(StartState, EndToken(RazorCommentKind));
    }

    protected StateResult StarAfterRazorCommentBody()
    {
        AssertCurrent('*');
        TakeCurrent();
        return Transition(
            RazorCommentTokenizerState.AtTokenAfterRazorCommentBody,
            EndToken(RazorCommentStarKind));
    }

    protected StateResult AtTokenAfterRazorCommentBody()
    {
        AssertCurrent('@');
        TakeCurrent();
        return Transition(StartState, EndToken(RazorCommentTransitionKind));
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

        using (var lookahead = BeginLookahead(Source))
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
        Debug.Assert(CurrentCharacter == current, "CurrentCharacter Assumption violated", "Assumed that the current character would be {0}, but it is actually {1}", current, CurrentCharacter);
    }

    protected enum RazorCommentTokenizerState
    {
        AfterRazorCommentTransition = 1000,
        EscapedRazorCommentTransition,
        RazorCommentBody,
        StarAfterRazorCommentBody,
        AtTokenAfterRazorCommentBody,
    }

    protected struct StateResult
    {
        public StateResult(int? state, SyntaxToken result)
        {
            State = state;
            Result = result;
        }

        public int? State { get; }

        public SyntaxToken Result { get; }
    }

    private static LookaheadToken BeginLookahead(ITextBuffer buffer)
    {
        var start = buffer.Position;
        return new LookaheadToken(buffer);
    }

    private struct LookaheadToken : IDisposable
    {
        private readonly ITextBuffer _buffer;
        private readonly int _position;

        private bool _accepted;

        public LookaheadToken(ITextBuffer buffer)
        {
            _buffer = buffer;
            _position = buffer.Position;

            _accepted = false;
        }

        public void Accept()
        {
            _accepted = true;
        }

        public void Dispose()
        {
            if (!_accepted)
            {
                _buffer.Position = _position;
            }
        }
    }
}
