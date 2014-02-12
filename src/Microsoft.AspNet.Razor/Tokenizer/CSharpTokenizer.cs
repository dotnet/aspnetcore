// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.AspNet.Razor.Parser;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.Text;
using Microsoft.AspNet.Razor.Tokenizer.Symbols;

namespace Microsoft.AspNet.Razor.Tokenizer
{
    public class CSharpTokenizer : Tokenizer<CSharpSymbol, CSharpSymbolType>
    {
        private Dictionary<char, Func<CSharpSymbolType>> _operatorHandlers;

        public CSharpTokenizer(ITextDocument source)
            : base(source)
        {
            CurrentState = Data;

            _operatorHandlers = new Dictionary<char, Func<CSharpSymbolType>>()
            {
                { '-', MinusOperator },
                { '<', LessThanOperator },
                { '>', GreaterThanOperator },
                { '&', CreateTwoCharOperatorHandler(CSharpSymbolType.And, '=', CSharpSymbolType.AndAssign, '&', CSharpSymbolType.DoubleAnd) },
                { '|', CreateTwoCharOperatorHandler(CSharpSymbolType.Or, '=', CSharpSymbolType.OrAssign, '|', CSharpSymbolType.DoubleOr) },
                { '+', CreateTwoCharOperatorHandler(CSharpSymbolType.Plus, '=', CSharpSymbolType.PlusAssign, '+', CSharpSymbolType.Increment) },
                { '=', CreateTwoCharOperatorHandler(CSharpSymbolType.Assign, '=', CSharpSymbolType.Equals, '>', CSharpSymbolType.GreaterThanEqual) },
                { '!', CreateTwoCharOperatorHandler(CSharpSymbolType.Not, '=', CSharpSymbolType.NotEqual) },
                { '%', CreateTwoCharOperatorHandler(CSharpSymbolType.Modulo, '=', CSharpSymbolType.ModuloAssign) },
                { '*', CreateTwoCharOperatorHandler(CSharpSymbolType.Star, '=', CSharpSymbolType.MultiplyAssign) },
                { ':', CreateTwoCharOperatorHandler(CSharpSymbolType.Colon, ':', CSharpSymbolType.DoubleColon) },
                { '?', CreateTwoCharOperatorHandler(CSharpSymbolType.QuestionMark, '?', CSharpSymbolType.NullCoalesce) },
                { '^', CreateTwoCharOperatorHandler(CSharpSymbolType.Xor, '=', CSharpSymbolType.XorAssign) },
                { '(', () => CSharpSymbolType.LeftParenthesis },
                { ')', () => CSharpSymbolType.RightParenthesis },
                { '{', () => CSharpSymbolType.LeftBrace },
                { '}', () => CSharpSymbolType.RightBrace },
                { '[', () => CSharpSymbolType.LeftBracket },
                { ']', () => CSharpSymbolType.RightBracket },
                { ',', () => CSharpSymbolType.Comma },
                { ';', () => CSharpSymbolType.Semicolon },
                { '~', () => CSharpSymbolType.Tilde },
                { '#', () => CSharpSymbolType.Hash }
            };
        }

        protected override State StartState
        {
            get { return Data; }
        }

        public override CSharpSymbolType RazorCommentType
        {
            get { return CSharpSymbolType.RazorComment; }
        }

        public override CSharpSymbolType RazorCommentTransitionType
        {
            get { return CSharpSymbolType.RazorCommentTransition; }
        }

        public override CSharpSymbolType RazorCommentStarType
        {
            get { return CSharpSymbolType.RazorCommentStar; }
        }

        protected override CSharpSymbol CreateSymbol(SourceLocation start, string content, CSharpSymbolType type, IEnumerable<RazorError> errors)
        {
            return new CSharpSymbol(start, content, type, errors);
        }

        private StateResult Data()
        {
            if (ParserHelpers.IsNewLine(CurrentCharacter))
            {
                // CSharp Spec §2.3.1
                bool checkTwoCharNewline = CurrentCharacter == '\r';
                TakeCurrent();
                if (checkTwoCharNewline && CurrentCharacter == '\n')
                {
                    TakeCurrent();
                }
                return Stay(EndSymbol(CSharpSymbolType.NewLine));
            }
            else if (ParserHelpers.IsWhitespace(CurrentCharacter))
            {
                // CSharp Spec §2.3.3
                TakeUntil(c => !ParserHelpers.IsWhitespace(c));
                return Stay(EndSymbol(CSharpSymbolType.WhiteSpace));
            }
            else if (CSharpHelpers.IsIdentifierStart(CurrentCharacter))
            {
                return Identifier();
            }
            else if (Char.IsDigit(CurrentCharacter))
            {
                return NumericLiteral();
            }
            switch (CurrentCharacter)
            {
                case '@':
                    return AtSymbol();
                case '\'':
                    TakeCurrent();
                    return Transition(() => QuotedLiteral('\'', CSharpSymbolType.CharacterLiteral));
                case '"':
                    TakeCurrent();
                    return Transition(() => QuotedLiteral('"', CSharpSymbolType.StringLiteral));
                case '.':
                    if (Char.IsDigit(Peek()))
                    {
                        return RealLiteral();
                    }
                    return Stay(Single(CSharpSymbolType.Dot));
                case '/':
                    TakeCurrent();
                    if (CurrentCharacter == '/')
                    {
                        TakeCurrent();
                        return SingleLineComment();
                    }
                    else if (CurrentCharacter == '*')
                    {
                        TakeCurrent();
                        return Transition(BlockComment);
                    }
                    else if (CurrentCharacter == '=')
                    {
                        TakeCurrent();
                        return Stay(EndSymbol(CSharpSymbolType.DivideAssign));
                    }
                    else
                    {
                        return Stay(EndSymbol(CSharpSymbolType.Slash));
                    }
                default:
                    return Stay(EndSymbol(Operator()));
            }
        }

        private StateResult AtSymbol()
        {
            TakeCurrent();
            if (CurrentCharacter == '"')
            {
                TakeCurrent();
                return Transition(VerbatimStringLiteral);
            }
            else if (CurrentCharacter == '*')
            {
                return Transition(EndSymbol(CSharpSymbolType.RazorCommentTransition), AfterRazorCommentTransition);
            }
            else if (CurrentCharacter == '@')
            {
                // Could be escaped comment transition
                return Transition(EndSymbol(CSharpSymbolType.Transition), () =>
                {
                    TakeCurrent();
                    return Transition(EndSymbol(CSharpSymbolType.Transition), Data);
                });
            }
            return Stay(EndSymbol(CSharpSymbolType.Transition));
        }

        private CSharpSymbolType Operator()
        {
            char first = CurrentCharacter;
            TakeCurrent();
            Func<CSharpSymbolType> handler;
            if (_operatorHandlers.TryGetValue(first, out handler))
            {
                return handler();
            }
            return CSharpSymbolType.Unknown;
        }

        private CSharpSymbolType LessThanOperator()
        {
            if (CurrentCharacter == '=')
            {
                TakeCurrent();
                return CSharpSymbolType.LessThanEqual;
            }
            return CSharpSymbolType.LessThan;
        }

        private CSharpSymbolType GreaterThanOperator()
        {
            if (CurrentCharacter == '=')
            {
                TakeCurrent();
                return CSharpSymbolType.GreaterThanEqual;
            }
            return CSharpSymbolType.GreaterThan;
        }

        private CSharpSymbolType MinusOperator()
        {
            if (CurrentCharacter == '>')
            {
                TakeCurrent();
                return CSharpSymbolType.Arrow;
            }
            else if (CurrentCharacter == '-')
            {
                TakeCurrent();
                return CSharpSymbolType.Decrement;
            }
            else if (CurrentCharacter == '=')
            {
                TakeCurrent();
                return CSharpSymbolType.MinusAssign;
            }
            return CSharpSymbolType.Minus;
        }

        private Func<CSharpSymbolType> CreateTwoCharOperatorHandler(CSharpSymbolType typeIfOnlyFirst, char second, CSharpSymbolType typeIfBoth)
        {
            return () =>
            {
                if (CurrentCharacter == second)
                {
                    TakeCurrent();
                    return typeIfBoth;
                }
                return typeIfOnlyFirst;
            };
        }

        private Func<CSharpSymbolType> CreateTwoCharOperatorHandler(CSharpSymbolType typeIfOnlyFirst, char option1, CSharpSymbolType typeIfOption1, char option2, CSharpSymbolType typeIfOption2)
        {
            return () =>
            {
                if (CurrentCharacter == option1)
                {
                    TakeCurrent();
                    return typeIfOption1;
                }
                else if (CurrentCharacter == option2)
                {
                    TakeCurrent();
                    return typeIfOption2;
                }
                return typeIfOnlyFirst;
            };
        }

        private StateResult VerbatimStringLiteral()
        {
            TakeUntil(c => c == '"');
            if (CurrentCharacter == '"')
            {
                TakeCurrent();
                if (CurrentCharacter == '"')
                {
                    TakeCurrent();
                    // Stay in the literal, this is an escaped "
                    return Stay();
                }
            }
            else if (EndOfFile)
            {
                CurrentErrors.Add(new RazorError(RazorResources.ParseError_Unterminated_String_Literal, CurrentStart));
            }
            return Transition(EndSymbol(CSharpSymbolType.StringLiteral), Data);
        }

        private StateResult QuotedLiteral(char quote, CSharpSymbolType literalType)
        {
            TakeUntil(c => c == '\\' || c == quote || ParserHelpers.IsNewLine(c));
            if (CurrentCharacter == '\\')
            {
                TakeCurrent(); // Take the '\'
                
                // If the next char is the same quote that started this
                if (CurrentCharacter == quote || CurrentCharacter == '\\')
                {
                    TakeCurrent(); // Take it so that we don't prematurely end the literal.
                }
                return Stay();
            }
            else if (EndOfFile || ParserHelpers.IsNewLine(CurrentCharacter))
            {
                CurrentErrors.Add(new RazorError(RazorResources.ParseError_Unterminated_String_Literal, CurrentStart));
            }
            else
            {
                TakeCurrent(); // No-op if at EOF
            }
            return Transition(EndSymbol(literalType), Data);
        }

        // CSharp Spec §2.3.2
        private StateResult BlockComment()
        {
            TakeUntil(c => c == '*');
            if (EndOfFile)
            {
                CurrentErrors.Add(new RazorError(RazorResources.ParseError_BlockComment_Not_Terminated, CurrentStart));
                return Transition(EndSymbol(CSharpSymbolType.Comment), Data);
            }
            if (CurrentCharacter == '*')
            {
                TakeCurrent();
                if (CurrentCharacter == '/')
                {
                    TakeCurrent();
                    return Transition(EndSymbol(CSharpSymbolType.Comment), Data);
                }
            }
            return Stay();
        }

        // CSharp Spec §2.3.2
        private StateResult SingleLineComment()
        {
            TakeUntil(c => ParserHelpers.IsNewLine(c));
            return Stay(EndSymbol(CSharpSymbolType.Comment));
        }

        // CSharp Spec §2.4.4
        private StateResult NumericLiteral()
        {
            if (TakeAll("0x", caseSensitive: true))
            {
                return HexLiteral();
            }
            else
            {
                return DecimalLiteral();
            }
        }

        private StateResult HexLiteral()
        {
            TakeUntil(c => !ParserHelpers.IsHexDigit(c));
            TakeIntegerSuffix();
            return Stay(EndSymbol(CSharpSymbolType.IntegerLiteral));
        }

        private StateResult DecimalLiteral()
        {
            TakeUntil(c => !Char.IsDigit(c));
            if (CurrentCharacter == '.' && Char.IsDigit(Peek()))
            {
                return RealLiteral();
            }
            else if (CSharpHelpers.IsRealLiteralSuffix(CurrentCharacter) ||
                     CurrentCharacter == 'E' || CurrentCharacter == 'e')
            {
                return RealLiteralExponentPart();
            }
            else
            {
                TakeIntegerSuffix();
                return Stay(EndSymbol(CSharpSymbolType.IntegerLiteral));
            }
        }

        private StateResult RealLiteralExponentPart()
        {
            if (CurrentCharacter == 'E' || CurrentCharacter == 'e')
            {
                TakeCurrent();
                if (CurrentCharacter == '+' || CurrentCharacter == '-')
                {
                    TakeCurrent();
                }
                TakeUntil(c => !Char.IsDigit(c));
            }
            if (CSharpHelpers.IsRealLiteralSuffix(CurrentCharacter))
            {
                TakeCurrent();
            }
            return Stay(EndSymbol(CSharpSymbolType.RealLiteral));
        }

        // CSharp Spec §2.4.4.3
        private StateResult RealLiteral()
        {
            AssertCurrent('.');
            TakeCurrent();
            Debug.Assert(Char.IsDigit(CurrentCharacter));
            TakeUntil(c => !Char.IsDigit(c));
            return RealLiteralExponentPart();
        }

        private void TakeIntegerSuffix()
        {
            if (Char.ToLowerInvariant(CurrentCharacter) == 'u')
            {
                TakeCurrent();
                if (Char.ToLowerInvariant(CurrentCharacter) == 'l')
                {
                    TakeCurrent();
                }
            }
            else if (Char.ToLowerInvariant(CurrentCharacter) == 'l')
            {
                TakeCurrent();
                if (Char.ToLowerInvariant(CurrentCharacter) == 'u')
                {
                    TakeCurrent();
                }
            }
        }

        // CSharp Spec §2.4.2
        private StateResult Identifier()
        {
            Debug.Assert(CSharpHelpers.IsIdentifierStart(CurrentCharacter));
            TakeCurrent();
            TakeUntil(c => !CSharpHelpers.IsIdentifierPart(c));
            CSharpSymbol sym = null;
            if (HaveContent)
            {
                CSharpKeyword? kwd = CSharpKeywordDetector.SymbolTypeForIdentifier(Buffer.ToString());
                CSharpSymbolType type = CSharpSymbolType.Identifier;
                if (kwd != null)
                {
                    type = CSharpSymbolType.Keyword;
                }
                sym = new CSharpSymbol(CurrentStart, Buffer.ToString(), type) { Keyword = kwd };
            }
            StartSymbol();
            return Stay(sym);
        }
    }
}
