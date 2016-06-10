// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.AspNetCore.Razor.Parser;
using Microsoft.AspNetCore.Razor.Text;
using Microsoft.AspNetCore.Razor.Tokenizer.Symbols.Internal;

namespace Microsoft.AspNetCore.Razor.Tokenizer.Internal
{
    // Tokenizer _loosely_ based on http://dev.w3.org/html5/spec/Overview.html#tokenization
    public class HtmlTokenizer : Tokenizer<HtmlSymbol, HtmlSymbolType>
    {
        private const char TransitionChar = '@';

        public HtmlTokenizer(ITextDocument source)
            : base(source)
        {
            base.CurrentState = StartState;
        }

        protected override int StartState => (int)HtmlTokenizerState.Data;

        private new HtmlTokenizerState? CurrentState => (HtmlTokenizerState?)base.CurrentState;

        public override HtmlSymbolType RazorCommentType
        {
            get { return HtmlSymbolType.RazorComment; }
        }

        public override HtmlSymbolType RazorCommentTransitionType
        {
            get { return HtmlSymbolType.RazorCommentTransition; }
        }

        public override HtmlSymbolType RazorCommentStarType
        {
            get { return HtmlSymbolType.RazorCommentStar; }
        }

        internal static IEnumerable<HtmlSymbol> Tokenize(string content)
        {
            using (SeekableTextReader reader = new SeekableTextReader(content))
            {
                var tokenizer = new HtmlTokenizer(reader);
                HtmlSymbol symbol;
                while ((symbol = tokenizer.NextSymbol()) != null)
                {
                    yield return symbol;
                }
            }
        }

        protected override HtmlSymbol CreateSymbol(SourceLocation start, string content, HtmlSymbolType type, IReadOnlyList<RazorError> errors)
        {
            return new HtmlSymbol(start, content, type, errors);
        }

        protected override StateResult Dispatch()
        {
            switch (CurrentState)
            {
                case HtmlTokenizerState.Data:
                    return Data();
                case HtmlTokenizerState.Text:
                    return Text();
                case HtmlTokenizerState.AfterRazorCommentTransition:
                    return AfterRazorCommentTransition();
                case HtmlTokenizerState.EscapedRazorCommentTransition:
                    return EscapedRazorCommentTransition();
                case HtmlTokenizerState.RazorCommentBody:
                    return RazorCommentBody();
                case HtmlTokenizerState.StarAfterRazorCommentBody:
                    return StarAfterRazorCommentBody();
                case HtmlTokenizerState.AtSymbolAfterRazorCommentBody:
                    return AtSymbolAfterRazorCommentBody();
                default:
#if NET451
                    // No Debug.Fail
                    Debug.Fail("Invalid TokenizerState");
#else
                    Debug.Assert(false, "Invalid TokenizerState");
#endif
                    return default(StateResult);
            }
        }

        // http://dev.w3.org/html5/spec/Overview.html#data-state
        private StateResult Data()
        {
            if (ParserHelpers.IsWhitespace(CurrentCharacter))
            {
                return Stay(Whitespace());
            }
            else if (ParserHelpers.IsNewLine(CurrentCharacter))
            {
                return Stay(Newline());
            }
            else if (CurrentCharacter == '@')
            {
                TakeCurrent();
                if (CurrentCharacter == '*')
                {
                    return Transition(
                        HtmlTokenizerState.AfterRazorCommentTransition,
                        EndSymbol(HtmlSymbolType.RazorCommentTransition));
                }
                else if (CurrentCharacter == '@')
                {
                    // Could be escaped comment transition
                    return Transition(
                        HtmlTokenizerState.EscapedRazorCommentTransition,
                        EndSymbol(HtmlSymbolType.Transition));
                }

                return Stay(EndSymbol(HtmlSymbolType.Transition));
            }
            else if (AtSymbol())
            {
                return Stay(Symbol());
            }
            else
            {
                return Transition(HtmlTokenizerState.Text);
            }
        }

        private StateResult EscapedRazorCommentTransition()
        {
            TakeCurrent();
            return Transition(HtmlTokenizerState.Data, EndSymbol(HtmlSymbolType.Transition));
        }

        private StateResult Text()
        {
            var prev = '\0';
            while (!EndOfFile && !ParserHelpers.IsWhitespaceOrNewLine(CurrentCharacter) && !AtSymbol())
            {
                prev = CurrentCharacter;
                TakeCurrent();
            }

            if (CurrentCharacter == '@')
            {
                var next = Peek();
                if (ParserHelpers.IsLetterOrDecimalDigit(prev) && ParserHelpers.IsLetterOrDecimalDigit(next))
                {
                    TakeCurrent(); // Take the "@"
                    return Stay(); // Stay in the Text state
                }
            }

            // Output the Text token and return to the Data state to tokenize the next character (if there is one)
            return Transition(HtmlTokenizerState.Data, EndSymbol(HtmlSymbolType.Text));
        }

        private HtmlSymbol Symbol()
        {
            Debug.Assert(AtSymbol());
            var sym = CurrentCharacter;
            TakeCurrent();
            switch (sym)
            {
                case '<':
                    return EndSymbol(HtmlSymbolType.OpenAngle);
                case '!':
                    return EndSymbol(HtmlSymbolType.Bang);
                case '/':
                    return EndSymbol(HtmlSymbolType.ForwardSlash);
                case '?':
                    return EndSymbol(HtmlSymbolType.QuestionMark);
                case '[':
                    return EndSymbol(HtmlSymbolType.LeftBracket);
                case '>':
                    return EndSymbol(HtmlSymbolType.CloseAngle);
                case ']':
                    return EndSymbol(HtmlSymbolType.RightBracket);
                case '=':
                    return EndSymbol(HtmlSymbolType.Equals);
                case '"':
                    return EndSymbol(HtmlSymbolType.DoubleQuote);
                case '\'':
                    return EndSymbol(HtmlSymbolType.SingleQuote);
                case '-':
                    Debug.Assert(CurrentCharacter == '-');
                    TakeCurrent();
                    return EndSymbol(HtmlSymbolType.DoubleHyphen);
                default:
#if NET451
                    // No Debug.Fail in CoreCLR

                    Debug.Fail("Unexpected symbol!");
#else
                    Debug.Assert(false, "Unexpected symbol");
#endif
                    return EndSymbol(HtmlSymbolType.Unknown);
            }
        }

        private HtmlSymbol Whitespace()
        {
            while (ParserHelpers.IsWhitespace(CurrentCharacter))
            {
                TakeCurrent();
            }
            return EndSymbol(HtmlSymbolType.WhiteSpace);
        }

        private HtmlSymbol Newline()
        {
            Debug.Assert(ParserHelpers.IsNewLine(CurrentCharacter));
            // CSharp Spec §2.3.1
            var checkTwoCharNewline = CurrentCharacter == '\r';
            TakeCurrent();
            if (checkTwoCharNewline && CurrentCharacter == '\n')
            {
                TakeCurrent();
            }
            return EndSymbol(HtmlSymbolType.NewLine);
        }

        private bool AtSymbol()
        {
            return CurrentCharacter == '<' ||
                   CurrentCharacter == '<' ||
                   CurrentCharacter == '!' ||
                   CurrentCharacter == '/' ||
                   CurrentCharacter == '?' ||
                   CurrentCharacter == '[' ||
                   CurrentCharacter == '>' ||
                   CurrentCharacter == ']' ||
                   CurrentCharacter == '=' ||
                   CurrentCharacter == '"' ||
                   CurrentCharacter == '\'' ||
                   CurrentCharacter == '@' ||
                   (CurrentCharacter == '-' && Peek() == '-');
        }

        private StateResult Transition(HtmlTokenizerState state)
        {
            return Transition((int)state, result: null);
        }

        private StateResult Transition(HtmlTokenizerState state, HtmlSymbol result)
        {
            return Transition((int)state, result);
        }

        private enum HtmlTokenizerState
        {
            Data,
            Text,

            // Razor Comments - need to be the same for HTML and CSharp
            AfterRazorCommentTransition = RazorCommentTokenizerState.AfterRazorCommentTransition,
            EscapedRazorCommentTransition = RazorCommentTokenizerState.EscapedRazorCommentTransition,
            RazorCommentBody = RazorCommentTokenizerState.RazorCommentBody,
            StarAfterRazorCommentBody = RazorCommentTokenizerState.StarAfterRazorCommentBody,
            AtSymbolAfterRazorCommentBody = RazorCommentTokenizerState.AtSymbolAfterRazorCommentBody,
        }
    }
}
