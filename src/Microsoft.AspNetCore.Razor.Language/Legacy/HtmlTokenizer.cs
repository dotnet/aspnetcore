// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.AspNetCore.Razor.Language.Syntax.InternalSyntax;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    // Tokenizer _loosely_ based on http://dev.w3.org/html5/spec/Overview.html#tokenization
    internal class HtmlTokenizer : Tokenizer
    {
        private const char TransitionChar = '@';

        public HtmlTokenizer(ITextDocument source)
            : base(source)
        {
            base.CurrentState = StartState;
        }

        protected override int StartState => (int)HtmlTokenizerState.Data;

        private new HtmlTokenizerState? CurrentState => (HtmlTokenizerState?)base.CurrentState;

        public override SyntaxKind RazorCommentKind
        {
            get { return SyntaxKind.RazorComment; }
        }

        public override SyntaxKind RazorCommentTransitionKind
        {
            get { return SyntaxKind.RazorCommentTransition; }
        }

        public override SyntaxKind RazorCommentStarKind
        {
            get { return SyntaxKind.RazorCommentStar; }
        }

        protected override SyntaxToken CreateToken(string content, SyntaxKind type, IReadOnlyList<RazorDiagnostic> errors)
        {
            return SyntaxFactory.Token(type, content, errors);
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
        protected override string GetTokenContent(SyntaxKind type)
        {
            var tokenLength = Buffer.Length;

            if (tokenLength == 1)
            {
                switch (type)
                {
                    case SyntaxKind.OpenAngle:
                        return "<";
                    case SyntaxKind.Bang:
                        return "!";
                    case SyntaxKind.ForwardSlash:
                        return "/";
                    case SyntaxKind.QuestionMark:
                        return "?";
                    case SyntaxKind.LeftBracket:
                        return "[";
                    case SyntaxKind.CloseAngle:
                        return ">";
                    case SyntaxKind.RightBracket:
                        return "]";
                    case SyntaxKind.Equals:
                        return "=";
                    case SyntaxKind.DoubleQuote:
                        return "\"";
                    case SyntaxKind.SingleQuote:
                        return "'";
                    case SyntaxKind.Whitespace:
                        if (Buffer[0] == ' ')
                        {
                            return " ";
                        }
                        if (Buffer[0] == '\t')
                        {
                            return "\t";
                        }
                        break;
                    case SyntaxKind.NewLine:
                        if (Buffer[0] == '\n')
                        {
                            return "\n";
                        }
                        break;
                }
            }

            if (tokenLength == 2 && type == SyntaxKind.NewLine)
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
                        EndToken(SyntaxKind.RazorCommentTransition));
                }
                else if (CurrentCharacter == '@')
                {
                    // Could be escaped comment transition
                    return Transition(
                        HtmlTokenizerState.EscapedRazorCommentTransition,
                        EndToken(SyntaxKind.Transition));
                }

                return Stay(EndToken(SyntaxKind.Transition));
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
            return Transition(HtmlTokenizerState.Data, EndToken(SyntaxKind.Transition));
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
            return Transition(HtmlTokenizerState.Data, EndToken(SyntaxKind.HtmlTextLiteral));
        }

        private SyntaxToken Token()
        {
            Debug.Assert(AtToken());
            var sym = CurrentCharacter;
            TakeCurrent();
            switch (sym)
            {
                case '<':
                    return EndToken(SyntaxKind.OpenAngle);
                case '!':
                    return EndToken(SyntaxKind.Bang);
                case '/':
                    return EndToken(SyntaxKind.ForwardSlash);
                case '?':
                    return EndToken(SyntaxKind.QuestionMark);
                case '[':
                    return EndToken(SyntaxKind.LeftBracket);
                case '>':
                    return EndToken(SyntaxKind.CloseAngle);
                case ']':
                    return EndToken(SyntaxKind.RightBracket);
                case '=':
                    return EndToken(SyntaxKind.Equals);
                case '"':
                    return EndToken(SyntaxKind.DoubleQuote);
                case '\'':
                    return EndToken(SyntaxKind.SingleQuote);
                case '-':
                    Debug.Assert(CurrentCharacter == '-');
                    TakeCurrent();
                    return EndToken(SyntaxKind.DoubleHyphen);
                default:
                    Debug.Fail("Unexpected token!");
                    return EndToken(SyntaxKind.Unknown);
            }
        }

        private SyntaxToken Whitespace()
        {
            while (ParserHelpers.IsWhitespace(CurrentCharacter))
            {
                TakeCurrent();
            }
            return EndToken(SyntaxKind.Whitespace);
        }

        private SyntaxToken Newline()
        {
            Debug.Assert(ParserHelpers.IsNewLine(CurrentCharacter));
            // CSharp Spec §2.3.1
            var checkTwoCharNewline = CurrentCharacter == '\r';
            TakeCurrent();
            if (checkTwoCharNewline && CurrentCharacter == '\n')
            {
                TakeCurrent();
            }
            return EndToken(SyntaxKind.NewLine);
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

        private StateResult Transition(HtmlTokenizerState state, SyntaxToken result)
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
