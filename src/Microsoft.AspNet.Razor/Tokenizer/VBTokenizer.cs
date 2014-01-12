// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using Microsoft.AspNet.Razor.Parser;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.Text;
using Microsoft.AspNet.Razor.Tokenizer.Symbols;

namespace Microsoft.AspNet.Razor.Tokenizer
{
    public class VBTokenizer : Tokenizer<VBSymbol, VBSymbolType>
    {
        private static Dictionary<char, VBSymbolType> _operatorTable = new Dictionary<char, VBSymbolType>()
        {
            { '_', VBSymbolType.LineContinuation },
            { '(', VBSymbolType.LeftParenthesis },
            { ')', VBSymbolType.RightParenthesis },
            { '[', VBSymbolType.LeftBracket },
            { ']', VBSymbolType.RightBracket },
            { '{', VBSymbolType.LeftBrace },
            { '}', VBSymbolType.RightBrace },
            { '!', VBSymbolType.Bang },
            { '#', VBSymbolType.Hash },
            { ',', VBSymbolType.Comma },
            { '.', VBSymbolType.Dot },
            { ':', VBSymbolType.Colon },
            { '?', VBSymbolType.QuestionMark },
            { '&', VBSymbolType.Concatenation },
            { '*', VBSymbolType.Multiply },
            { '+', VBSymbolType.Add },
            { '-', VBSymbolType.Subtract },
            { '/', VBSymbolType.Divide },
            { '\\', VBSymbolType.IntegerDivide },
            { '^', VBSymbolType.Exponentiation },
            { '=', VBSymbolType.Equal },
            { '<', VBSymbolType.LessThan },
            { '>', VBSymbolType.GreaterThan },
            { '$', VBSymbolType.Dollar },
        };

        public VBTokenizer(ITextDocument source)
            : base(source)
        {
            CurrentState = Data;
        }

        protected override State StartState
        {
            get { return Data; }
        }

        public override VBSymbolType RazorCommentType
        {
            get { return VBSymbolType.RazorComment; }
        }

        public override VBSymbolType RazorCommentTransitionType
        {
            get { return VBSymbolType.RazorCommentTransition; }
        }

        public override VBSymbolType RazorCommentStarType
        {
            get { return VBSymbolType.RazorCommentStar; }
        }

        internal static IEnumerable<VBSymbol> Tokenize(string content)
        {
            using (SeekableTextReader reader = new SeekableTextReader(content))
            {
                VBTokenizer tok = new VBTokenizer(reader);
                VBSymbol sym;
                while ((sym = tok.NextSymbol()) != null)
                {
                    yield return sym;
                }
            }
        }

        protected override VBSymbol CreateSymbol(SourceLocation start, string content, VBSymbolType type, IEnumerable<RazorError> errors)
        {
            return new VBSymbol(start, content, type, errors);
        }

        private StateResult Data()
        {
            // We are accepting more characters and whitespace/newlines then the VB Spec defines, to simplify things
            // Since the code must still be compiled by a VB compiler, this will not cause adverse effects.
            if (ParserHelpers.IsNewLine(CurrentCharacter))
            {
                // VB Spec §2.1.1
                bool checkTwoCharNewline = CurrentCharacter == '\r';
                TakeCurrent();
                if (checkTwoCharNewline && CurrentCharacter == '\n')
                {
                    TakeCurrent();
                }
                return Stay(EndSymbol(VBSymbolType.NewLine));
            }
            else if (ParserHelpers.IsWhitespace(CurrentCharacter))
            {
                // CSharp Spec §2.1.3
                TakeUntil(c => !ParserHelpers.IsWhitespace(c));
                return Stay(EndSymbol(VBSymbolType.WhiteSpace));
            }
            else if (VBHelpers.IsSingleQuote(CurrentCharacter))
            {
                TakeCurrent();
                return CommentBody();
            }
            else if (IsIdentifierStart())
            {
                return Identifier();
            }
            else if (Char.IsDigit(CurrentCharacter))
            {
                return DecimalLiteral();
            }
            else if (CurrentCharacter == '&')
            {
                char next = Char.ToLower(Peek(), CultureInfo.InvariantCulture);
                if (next == 'h')
                {
                    return HexLiteral();
                }
                else if (next == 'o')
                {
                    return OctLiteral();
                }
            }
            else if (CurrentCharacter == '.' && Char.IsDigit(Peek()))
            {
                return FloatingPointLiteralEnd();
            }
            else if (VBHelpers.IsDoubleQuote(CurrentCharacter))
            {
                TakeCurrent();
                return Transition(QuotedLiteral);
            }
            else if (AtDateLiteral())
            {
                return DateLiteral();
            }
            else if (CurrentCharacter == '@')
            {
                TakeCurrent();
                if (CurrentCharacter == '*')
                {
                    return Transition(EndSymbol(VBSymbolType.RazorCommentTransition), AfterRazorCommentTransition);
                }
                else if (CurrentCharacter == '@')
                {
                    // Could be escaped comment transition
                    return Transition(EndSymbol(VBSymbolType.Transition), () =>
                    {
                        TakeCurrent();
                        return Transition(EndSymbol(VBSymbolType.Transition), Data);
                    });
                }
                else
                {
                    return Stay(EndSymbol(VBSymbolType.Transition));
                }
            }
            return Stay(EndSymbol(Operator()));
        }

        private StateResult DateLiteral()
        {
            AssertCurrent('#');
            TakeCurrent();
            TakeUntil(c => c == '#' || ParserHelpers.IsNewLine(c));
            if (CurrentCharacter == '#')
            {
                TakeCurrent();
            }
            return Stay(EndSymbol(VBSymbolType.DateLiteral));
        }

        private bool AtDateLiteral()
        {
            if (CurrentCharacter != '#')
            {
                return false;
            }
            int start = Source.Position;
            try
            {
                MoveNext();
                while (ParserHelpers.IsWhitespace(CurrentCharacter))
                {
                    MoveNext();
                }
                return Char.IsDigit(CurrentCharacter);
            }
            finally
            {
                Source.Position = start;
            }
        }

        private StateResult QuotedLiteral()
        {
            TakeUntil(c => VBHelpers.IsDoubleQuote(c) || ParserHelpers.IsNewLine(c));
            if (VBHelpers.IsDoubleQuote(CurrentCharacter))
            {
                TakeCurrent();
                if (VBHelpers.IsDoubleQuote(CurrentCharacter))
                {
                    // Escape sequence, remain in the string
                    TakeCurrent();
                    return Stay();
                }
            }

            VBSymbolType type = VBSymbolType.StringLiteral;
            if (Char.ToLowerInvariant(CurrentCharacter) == 'c')
            {
                TakeCurrent();
                type = VBSymbolType.CharacterLiteral;
            }
            return Transition(EndSymbol(type), Data);
        }

        private StateResult DecimalLiteral()
        {
            TakeUntil(c => !Char.IsDigit(c));
            char lower = Char.ToLowerInvariant(CurrentCharacter);
            if (IsFloatTypeSuffix(lower) || lower == '.' || lower == 'e')
            {
                return FloatingPointLiteralEnd();
            }
            else
            {
                TakeIntTypeSuffix();
                return Stay(EndSymbol(VBSymbolType.IntegerLiteral));
            }
        }

        private static bool IsFloatTypeSuffix(char chr)
        {
            chr = Char.ToLowerInvariant(chr);
            return chr == 'f' || chr == 'r' || chr == 'd';
        }

        private StateResult FloatingPointLiteralEnd()
        {
            if (CurrentCharacter == '.')
            {
                TakeCurrent();
                TakeUntil(c => !Char.IsDigit(c));
            }
            if (Char.ToLowerInvariant(CurrentCharacter) == 'e')
            {
                TakeCurrent();
                if (CurrentCharacter == '+' || CurrentCharacter == '-')
                {
                    TakeCurrent();
                }
                TakeUntil(c => !Char.IsDigit(c));
            }
            if (IsFloatTypeSuffix(CurrentCharacter))
            {
                TakeCurrent();
            }
            return Stay(EndSymbol(VBSymbolType.FloatingPointLiteral));
        }

        private StateResult HexLiteral()
        {
            AssertCurrent('&');
            TakeCurrent();
            Debug.Assert(Char.ToLowerInvariant(CurrentCharacter) == 'h');
            TakeCurrent();
            TakeUntil(c => !ParserHelpers.IsHexDigit(c));
            TakeIntTypeSuffix();
            return Stay(EndSymbol(VBSymbolType.IntegerLiteral));
        }

        private StateResult OctLiteral()
        {
            AssertCurrent('&');
            TakeCurrent();
            Debug.Assert(Char.ToLowerInvariant(CurrentCharacter) == 'o');
            TakeCurrent();
            TakeUntil(c => !VBHelpers.IsOctalDigit(c));
            TakeIntTypeSuffix();
            return Stay(EndSymbol(VBSymbolType.IntegerLiteral));
        }

        private VBSymbolType Operator()
        {
            char op = CurrentCharacter;
            TakeCurrent();
            VBSymbolType ret;
            if (_operatorTable.TryGetValue(op, out ret))
            {
                return ret;
            }
            return VBSymbolType.Unknown;
        }

        private void TakeIntTypeSuffix()
        {
            // Take the "U" in US, UI, UL
            if (Char.ToLowerInvariant(CurrentCharacter) == 'u')
            {
                TakeCurrent(); // Unsigned Prefix
            }

            // Take the S, I or L integer suffix
            if (IsIntegerSuffix(CurrentCharacter))
            {
                TakeCurrent();
            }
        }

        private static bool IsIntegerSuffix(char chr)
        {
            chr = Char.ToLowerInvariant(chr);
            return chr == 's' || chr == 'i' || chr == 'l';
        }

        private StateResult CommentBody()
        {
            TakeUntil(ParserHelpers.IsNewLine);
            return Stay(EndSymbol(VBSymbolType.Comment));
        }

        private StateResult Identifier()
        {
            bool isEscaped = false;
            if (CurrentCharacter == '[')
            {
                TakeCurrent();
                isEscaped = true;
            }
            TakeUntil(c => !ParserHelpers.IsIdentifierPart(c));

            // If we're escaped, take the ']'
            if (isEscaped)
            {
                if (CurrentCharacter == ']')
                {
                    TakeCurrent();
                }
            }

            // Check for Keywords and build the symbol
            VBKeyword? keyword = VBKeywordDetector.GetKeyword(Buffer.ToString());
            if (keyword == VBKeyword.Rem)
            {
                return CommentBody();
            }

            VBSymbol sym = new VBSymbol(CurrentStart, Buffer.ToString(), keyword == null ? VBSymbolType.Identifier : VBSymbolType.Keyword)
            {
                Keyword = keyword
            };

            StartSymbol();

            return Stay(sym);
        }

        private bool IsIdentifierStart()
        {
            if (CurrentCharacter == '_')
            {
                // VB Spec §2.2:
                //  If an identifier begins with an underscore, it must contain at least one other valid identifier character to disambiguate it from a line continuation.
                return ParserHelpers.IsIdentifierPart(Peek());
            }
            if (CurrentCharacter == '[')
            {
                return ParserHelpers.IsIdentifierPart(Peek());
            }
            return ParserHelpers.IsIdentifierStart(CurrentCharacter);
        }
    }
}
