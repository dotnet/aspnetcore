// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    // Tokenizer _loosely_ based on http://dev.w3.org/html5/spec/Overview.html#tokenization
    internal class HtmlTokenizer : Tokenizer<HtmlToken, HtmlTokenType>
    {
        private const char TransitionChar = '@';

        public HtmlTokenizer(ITextDocument source)
            : base(source)
        {
            base.CurrentState = StartState;
        }

        protected override int StartState => (int)HtmlTokenizerState.Data;

        private new HtmlTokenizerState? CurrentState => (HtmlTokenizerState?)base.CurrentState;

        public override HtmlTokenType RazorCommentType
        {
            get { return HtmlTokenType.RazorComment; }
        }

        public override HtmlTokenType RazorCommentTransitionType
        {
            get { return HtmlTokenType.RazorCommentTransition; }
        }

        public override HtmlTokenType RazorCommentStarType
        {
            get { return HtmlTokenType.RazorCommentStar; }
        }

        protected override HtmlToken CreateToken(string content, HtmlTokenType type, IReadOnlyList<RazorDiagnostic> errors)
        {
            return new HtmlToken(content, type, errors);
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
                case HtmlTokenizerState.AtTokenAfterRazorCommentBody:
                    return AtTokenAfterRazorCommentBody();
                default:
                    Debug.Fail("Invalid TokenizerState");
                    return default(StateResult);
            }
        }

        // Optimize memory allocation by returning constants for the most frequent cases
        protected override string GetTokenContent(HtmlTokenType type)
        {
            var tokenLength = Buffer.Length;

            if (tokenLength == 1)
            {
                switch (type)
                {
                    case HtmlTokenType.OpenAngle:
                        return "<";
                    case HtmlTokenType.Bang:
                        return "!";
                    case HtmlTokenType.ForwardSlash:
                        return "/";
                    case HtmlTokenType.QuestionMark:
                        return "?";
                    case HtmlTokenType.LeftBracket:
                        return "[";
                    case HtmlTokenType.CloseAngle:
                        return ">";
                    case HtmlTokenType.RightBracket:
                        return "]";
                    case HtmlTokenType.Equals:
                        return "=";
                    case HtmlTokenType.DoubleQuote:
                        return "\"";
                    case HtmlTokenType.SingleQuote:
                        return "'";
                    case HtmlTokenType.WhiteSpace:
                        if (Buffer[0] == ' ')
                        {
                            return " ";
                        }
                        if (Buffer[0] == '\t')
                        {
                            return "\t";
                        }
                        break;
                    case HtmlTokenType.NewLine:
                        if (Buffer[0] == '\n')
                        {
                            return "\n";
                        }
                        break;
                }
            }

            if (tokenLength == 2 && type == HtmlTokenType.NewLine)
            {
                return "\r\n";
            }

            return base.GetTokenContent(type);
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
                        EndToken(HtmlTokenType.RazorCommentTransition));
                }
                else if (CurrentCharacter == '@')
                {
                    // Could be escaped comment transition
                    return Transition(
                        HtmlTokenizerState.EscapedRazorCommentTransition,
                        EndToken(HtmlTokenType.Transition));
                }

                return Stay(EndToken(HtmlTokenType.Transition));
            }
            else if (AtToken())
            {
                return Stay(Token());
            }
            else
            {
                return Transition(HtmlTokenizerState.Text);
            }
        }

        private StateResult EscapedRazorCommentTransition()
        {
            TakeCurrent();
            return Transition(HtmlTokenizerState.Data, EndToken(HtmlTokenType.Transition));
        }

        private StateResult Text()
        {
            var prev = '\0';
            while (!EndOfFile &&
                !(ParserHelpers.IsWhitespace(CurrentCharacter) || ParserHelpers.IsNewLine(CurrentCharacter)) &&
                !AtToken())
            {
                prev = CurrentCharacter;
                TakeCurrent();
            }

            if (CurrentCharacter == '@')
            {
                var next = Peek();
                if ((ParserHelpers.IsLetter(prev) || ParserHelpers.IsDecimalDigit(prev)) &&
                    (ParserHelpers.IsLetter(next) || ParserHelpers.IsDecimalDigit(next)))
                {
                    TakeCurrent(); // Take the "@"
                    return Stay(); // Stay in the Text state
                }
            }

            // Output the Text token and return to the Data state to tokenize the next character (if there is one)
            return Transition(HtmlTokenizerState.Data, EndToken(HtmlTokenType.Text));
        }

        private HtmlToken Token()
        {
            Debug.Assert(AtToken());
            var sym = CurrentCharacter;
            TakeCurrent();
            switch (sym)
            {
                case '<':
                    return EndToken(HtmlTokenType.OpenAngle);
                case '!':
                    return EndToken(HtmlTokenType.Bang);
                case '/':
                    return EndToken(HtmlTokenType.ForwardSlash);
                case '?':
                    return EndToken(HtmlTokenType.QuestionMark);
                case '[':
                    return EndToken(HtmlTokenType.LeftBracket);
                case '>':
                    return EndToken(HtmlTokenType.CloseAngle);
                case ']':
                    return EndToken(HtmlTokenType.RightBracket);
                case '=':
                    return EndToken(HtmlTokenType.Equals);
                case '"':
                    return EndToken(HtmlTokenType.DoubleQuote);
                case '\'':
                    return EndToken(HtmlTokenType.SingleQuote);
                case '-':
                    Debug.Assert(CurrentCharacter == '-');
                    TakeCurrent();
                    return EndToken(HtmlTokenType.DoubleHyphen);
                default:
                    Debug.Fail("Unexpected token!");
                    return EndToken(HtmlTokenType.Unknown);
            }
        }

        private HtmlToken Whitespace()
        {
            while (ParserHelpers.IsWhitespace(CurrentCharacter))
            {
                TakeCurrent();
            }
            return EndToken(HtmlTokenType.WhiteSpace);
        }

        private HtmlToken Newline()
        {
            Debug.Assert(ParserHelpers.IsNewLine(CurrentCharacter));
            // CSharp Spec §2.3.1
            var checkTwoCharNewline = CurrentCharacter == '\r';
            TakeCurrent();
            if (checkTwoCharNewline && CurrentCharacter == '\n')
            {
                TakeCurrent();
            }
            return EndToken(HtmlTokenType.NewLine);
        }

        private bool AtToken()
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

        private StateResult Transition(HtmlTokenizerState state, HtmlToken result)
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
            AtTokenAfterRazorCommentBody = RazorCommentTokenizerState.AtTokenAfterRazorCommentBody,
        }
    }
}
