// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;

namespace Microsoft.AspNetCore.Razor.Evolution.Legacy
{
    internal class CSharpTokenizer : Tokenizer<CSharpSymbol, CSharpSymbolType>
    {
        private Dictionary<char, Func<CSharpSymbolType>> _operatorHandlers;

        private static readonly Dictionary<string, CSharpKeyword> _keywords = new Dictionary<string, CSharpKeyword>(StringComparer.Ordinal)
        {
            { "await", CSharpKeyword.Await },
            { "abstract", CSharpKeyword.Abstract },
            { "byte", CSharpKeyword.Byte },
            { "class", CSharpKeyword.Class },
            { "delegate", CSharpKeyword.Delegate },
            { "event", CSharpKeyword.Event },
            { "fixed", CSharpKeyword.Fixed },
            { "if", CSharpKeyword.If },
            { "internal", CSharpKeyword.Internal },
            { "new", CSharpKeyword.New },
            { "override", CSharpKeyword.Override },
            { "readonly", CSharpKeyword.Readonly },
            { "short", CSharpKeyword.Short },
            { "struct", CSharpKeyword.Struct },
            { "try", CSharpKeyword.Try },
            { "unsafe", CSharpKeyword.Unsafe },
            { "volatile", CSharpKeyword.Volatile },
            { "as", CSharpKeyword.As },
            { "do", CSharpKeyword.Do },
            { "is", CSharpKeyword.Is },
            { "params", CSharpKeyword.Params },
            { "ref", CSharpKeyword.Ref },
            { "switch", CSharpKeyword.Switch },
            { "ushort", CSharpKeyword.Ushort },
            { "while", CSharpKeyword.While },
            { "case", CSharpKeyword.Case },
            { "const", CSharpKeyword.Const },
            { "explicit", CSharpKeyword.Explicit },
            { "float", CSharpKeyword.Float },
            { "null", CSharpKeyword.Null },
            { "sizeof", CSharpKeyword.Sizeof },
            { "typeof", CSharpKeyword.Typeof },
            { "implicit", CSharpKeyword.Implicit },
            { "private", CSharpKeyword.Private },
            { "this", CSharpKeyword.This },
            { "using", CSharpKeyword.Using },
            { "extern", CSharpKeyword.Extern },
            { "return", CSharpKeyword.Return },
            { "stackalloc", CSharpKeyword.Stackalloc },
            { "uint", CSharpKeyword.Uint },
            { "base", CSharpKeyword.Base },
            { "catch", CSharpKeyword.Catch },
            { "continue", CSharpKeyword.Continue },
            { "double", CSharpKeyword.Double },
            { "for", CSharpKeyword.For },
            { "in", CSharpKeyword.In },
            { "lock", CSharpKeyword.Lock },
            { "object", CSharpKeyword.Object },
            { "protected", CSharpKeyword.Protected },
            { "static", CSharpKeyword.Static },
            { "false", CSharpKeyword.False },
            { "public", CSharpKeyword.Public },
            { "sbyte", CSharpKeyword.Sbyte },
            { "throw", CSharpKeyword.Throw },
            { "virtual", CSharpKeyword.Virtual },
            { "decimal", CSharpKeyword.Decimal },
            { "else", CSharpKeyword.Else },
            { "operator", CSharpKeyword.Operator },
            { "string", CSharpKeyword.String },
            { "ulong", CSharpKeyword.Ulong },
            { "bool", CSharpKeyword.Bool },
            { "char", CSharpKeyword.Char },
            { "default", CSharpKeyword.Default },
            { "foreach", CSharpKeyword.Foreach },
            { "long", CSharpKeyword.Long },
            { "void", CSharpKeyword.Void },
            { "enum", CSharpKeyword.Enum },
            { "finally", CSharpKeyword.Finally },
            { "int", CSharpKeyword.Int },
            { "out", CSharpKeyword.Out },
            { "sealed", CSharpKeyword.Sealed },
            { "true", CSharpKeyword.True },
            { "goto", CSharpKeyword.Goto },
            { "unchecked", CSharpKeyword.Unchecked },
            { "interface", CSharpKeyword.Interface },
            { "break", CSharpKeyword.Break },
            { "checked", CSharpKeyword.Checked },
            { "namespace", CSharpKeyword.Namespace },
            { "when", CSharpKeyword.When }
        };

        public CSharpTokenizer(ITextDocument source)
            : base(source)
        {
            base.CurrentState = StartState;

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

        protected override int StartState => (int)CSharpTokenizerState.Data;

        private new CSharpTokenizerState? CurrentState => (CSharpTokenizerState?)base.CurrentState;

        public override CSharpSymbolType RazorCommentType => CSharpSymbolType.RazorComment;

        public override CSharpSymbolType RazorCommentTransitionType => CSharpSymbolType.RazorCommentTransition;

        public override CSharpSymbolType RazorCommentStarType => CSharpSymbolType.RazorCommentStar;

        protected override StateResult Dispatch()
        {
            switch (CurrentState)
            {
                case CSharpTokenizerState.Data:
                    return Data();
                case CSharpTokenizerState.BlockComment:
                    return BlockComment();
                case CSharpTokenizerState.QuotedCharacterLiteral:
                    return QuotedCharacterLiteral();
                case CSharpTokenizerState.QuotedStringLiteral:
                    return QuotedStringLiteral();
                case CSharpTokenizerState.VerbatimStringLiteral:
                    return VerbatimStringLiteral();
                case CSharpTokenizerState.AfterRazorCommentTransition:
                    return AfterRazorCommentTransition();
                case CSharpTokenizerState.EscapedRazorCommentTransition:
                    return EscapedRazorCommentTransition();
                case CSharpTokenizerState.RazorCommentBody:
                    return RazorCommentBody();
                case CSharpTokenizerState.StarAfterRazorCommentBody:
                    return StarAfterRazorCommentBody();
                case CSharpTokenizerState.AtSymbolAfterRazorCommentBody:
                    return AtSymbolAfterRazorCommentBody();
                default:
                    Debug.Fail("Invalid TokenizerState");
                    return default(StateResult);
            }
        }

        // Optimize memory allocation by returning constants for the most frequent cases
        protected override string GetSymbolContent(CSharpSymbolType type)
        {
            var symbolLength = Buffer.Length;

            if (symbolLength == 1)
            {
                switch (type)
                {
                    case CSharpSymbolType.IntegerLiteral:
                        switch (Buffer[0])
                        {
                            case '0':
                                return "0";
                            case '1':
                                return "1";
                            case '2':
                                return "2";
                            case '3':
                                return "3";
                            case '4':
                                return "4";
                            case '5':
                                return "5";
                            case '6':
                                return "6";
                            case '7':
                                return "7";
                            case '8':
                                return "8";
                            case '9':
                                return "9";
                        }
                        break;
                    case CSharpSymbolType.NewLine:
                        if (Buffer[0] == '\n')
                        {
                            return "\n";
                        }
                        break;
                    case CSharpSymbolType.WhiteSpace:
                        if (Buffer[0] == ' ')
                        {
                            return " ";
                        }
                        if (Buffer[0] == '\t')
                        {
                            return "\t";
                        }
                        break;
                    case CSharpSymbolType.Minus:
                        return "-";
                    case CSharpSymbolType.Not:
                        return "!";
                    case CSharpSymbolType.Modulo:
                        return "%";
                    case CSharpSymbolType.And:
                        return "&";
                    case CSharpSymbolType.LeftParenthesis:
                        return "(";
                    case CSharpSymbolType.RightParenthesis:
                        return ")";
                    case CSharpSymbolType.Star:
                        return "*";
                    case CSharpSymbolType.Comma:
                        return ",";
                    case CSharpSymbolType.Dot:
                        return ".";
                    case CSharpSymbolType.Slash:
                        return "/";
                    case CSharpSymbolType.Colon:
                        return ":";
                    case CSharpSymbolType.Semicolon:
                        return ";";
                    case CSharpSymbolType.QuestionMark:
                        return "?";
                    case CSharpSymbolType.RightBracket:
                        return "]";
                    case CSharpSymbolType.LeftBracket:
                        return "[";
                    case CSharpSymbolType.Xor:
                        return "^";
                    case CSharpSymbolType.LeftBrace:
                        return "{";
                    case CSharpSymbolType.Or:
                        return "|";
                    case CSharpSymbolType.RightBrace:
                        return "}";
                    case CSharpSymbolType.Tilde:
                        return "~";
                    case CSharpSymbolType.Plus:
                        return "+";
                    case CSharpSymbolType.LessThan:
                        return "<";
                    case CSharpSymbolType.Assign:
                        return "=";
                    case CSharpSymbolType.GreaterThan:
                        return ">";
                    case CSharpSymbolType.Hash:
                        return "#";
                    case CSharpSymbolType.Transition:
                        return "@";

                }
            }
            else if (symbolLength == 2)
            {
                switch (type)
                {
                    case CSharpSymbolType.NewLine:
                        return "\r\n";
                    case CSharpSymbolType.Arrow:
                        return "->";
                    case CSharpSymbolType.Decrement:
                        return "--";
                    case CSharpSymbolType.MinusAssign:
                        return "-=";
                    case CSharpSymbolType.NotEqual:
                        return "!=";
                    case CSharpSymbolType.ModuloAssign:
                        return "%=";
                    case CSharpSymbolType.AndAssign:
                        return "&=";
                    case CSharpSymbolType.DoubleAnd:
                        return "&&";
                    case CSharpSymbolType.MultiplyAssign:
                        return "*=";
                    case CSharpSymbolType.DivideAssign:
                        return "/=";
                    case CSharpSymbolType.DoubleColon:
                        return "::";
                    case CSharpSymbolType.NullCoalesce:
                        return "??";
                    case CSharpSymbolType.XorAssign:
                        return "^=";
                    case CSharpSymbolType.OrAssign:
                        return "|=";
                    case CSharpSymbolType.DoubleOr:
                        return "||";
                    case CSharpSymbolType.PlusAssign:
                        return "+=";
                    case CSharpSymbolType.Increment:
                        return "++";
                    case CSharpSymbolType.LessThanEqual:
                        return "<=";
                    case CSharpSymbolType.LeftShift:
                        return "<<";
                    case CSharpSymbolType.Equals:
                        return "==";
                    case CSharpSymbolType.GreaterThanEqual:
                        if (Buffer[0] == '=')
                        {
                            return "=>";
                        }
                        return ">=";
                    case CSharpSymbolType.RightShift:
                        return ">>";


                }
            }
            else if (symbolLength == 3)
            {
                switch (type)
                {
                    case CSharpSymbolType.LeftShiftAssign:
                        return "<<=";
                    case CSharpSymbolType.RightShiftAssign:
                        return ">>=";
                }
            }

            return base.GetSymbolContent(type);
        }

        protected override CSharpSymbol CreateSymbol(string content, CSharpSymbolType type, IReadOnlyList<RazorError> errors)
        {
            return new CSharpSymbol(content, type, errors);
        }

        private StateResult Data()
        {
            if (ParserHelpers.IsNewLine(CurrentCharacter))
            {
                // CSharp Spec §2.3.1
                var checkTwoCharNewline = CurrentCharacter == '\r';
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
            else if (IsIdentifierStart(CurrentCharacter))
            {
                return Identifier();
            }
            else if (char.IsDigit(CurrentCharacter))
            {
                return NumericLiteral();
            }
            switch (CurrentCharacter)
            {
                case '@':
                    return AtSymbol();
                case '\'':
                    TakeCurrent();
                    return Transition(CSharpTokenizerState.QuotedCharacterLiteral);
                case '"':
                    TakeCurrent();
                    return Transition(CSharpTokenizerState.QuotedStringLiteral);
                case '.':
                    if (char.IsDigit(Peek()))
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
                        return Transition(CSharpTokenizerState.BlockComment);
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
                return Transition(CSharpTokenizerState.VerbatimStringLiteral);
            }
            else if (CurrentCharacter == '*')
            {
                return Transition(
                    CSharpTokenizerState.AfterRazorCommentTransition,
                    EndSymbol(CSharpSymbolType.RazorCommentTransition));
            }
            else if (CurrentCharacter == '@')
            {
                // Could be escaped comment transition
                return Transition(
                    CSharpTokenizerState.EscapedRazorCommentTransition,
                    EndSymbol(CSharpSymbolType.Transition));
            }

            return Stay(EndSymbol(CSharpSymbolType.Transition));
        }

        private StateResult EscapedRazorCommentTransition()
        {
            TakeCurrent();
            return Transition(CSharpTokenizerState.Data, EndSymbol(CSharpSymbolType.Transition));
        }

        private CSharpSymbolType Operator()
        {
            var first = CurrentCharacter;
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
                CurrentErrors.Add(
                    new RazorError(
                        LegacyResources.ParseError_Unterminated_String_Literal,
                        CurrentStart,
                        length: 1 /* end of file */));
            }
            return Transition(CSharpTokenizerState.Data, EndSymbol(CSharpSymbolType.StringLiteral));
        }

        private StateResult QuotedCharacterLiteral() => QuotedLiteral('\'', CSharpSymbolType.CharacterLiteral);

        private StateResult QuotedStringLiteral() => QuotedLiteral('\"', CSharpSymbolType.StringLiteral);

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
                CurrentErrors.Add(
                    new RazorError(
                        LegacyResources.ParseError_Unterminated_String_Literal,
                        CurrentStart,
                        length: 1 /* " */));
            }
            else
            {
                TakeCurrent(); // No-op if at EOF
            }
            return Transition(CSharpTokenizerState.Data, EndSymbol(literalType));
        }

        // CSharp Spec §2.3.2
        private StateResult BlockComment()
        {
            TakeUntil(c => c == '*');
            if (EndOfFile)
            {
                CurrentErrors.Add(
                    new RazorError(
                        LegacyResources.ParseError_BlockComment_Not_Terminated,
                        CurrentStart,
                        length: 1 /* end of file */));
                return Transition(CSharpTokenizerState.Data, EndSymbol(CSharpSymbolType.Comment));
            }
            if (CurrentCharacter == '*')
            {
                TakeCurrent();
                if (CurrentCharacter == '/')
                {
                    TakeCurrent();
                    return Transition(CSharpTokenizerState.Data, EndSymbol(CSharpSymbolType.Comment));
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
            TakeUntil(c => !IsHexDigit(c));
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
            else if (IsRealLiteralSuffix(CurrentCharacter) ||
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
            if (IsRealLiteralSuffix(CurrentCharacter))
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
            Debug.Assert(IsIdentifierStart(CurrentCharacter));
            TakeCurrent();
            TakeUntil(c => !IsIdentifierPart(c));
            CSharpSymbol symbol = null;
            if (HaveContent)
            {
                CSharpKeyword keyword;
                var type = CSharpSymbolType.Identifier;
                var symbolContent = Buffer.ToString();
                if (_keywords.TryGetValue(symbolContent, out keyword))
                {
                    type = CSharpSymbolType.Keyword;
                }
                
                symbol = new CSharpSymbol(symbolContent, type)
                {
                    Keyword = type == CSharpSymbolType.Keyword ? (CSharpKeyword?)keyword : null,
                };
                
                Buffer.Clear();
                CurrentErrors.Clear();
            }

            return Stay(symbol);
        }

        private StateResult Transition(CSharpTokenizerState state)
        {
            return Transition((int)state, result: null);
        }

        private StateResult Transition(CSharpTokenizerState state, CSharpSymbol result)
        {
            return Transition((int)state, result);
        }

        private static bool IsIdentifierStart(char character)
        {
            return char.IsLetter(character) ||
                   character == '_' ||
                   CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.LetterNumber;
        }

        private static bool IsIdentifierPart(char character)
        {
            return char.IsDigit(character) ||
                   IsIdentifierStart(character) ||
                   IsIdentifierPartByUnicodeCategory(character);
        }

        private static bool IsRealLiteralSuffix(char character)
        {
            return character == 'F' ||
                   character == 'f' ||
                   character == 'D' ||
                   character == 'd' ||
                   character == 'M' ||
                   character == 'm';
        }

        private static bool IsIdentifierPartByUnicodeCategory(char character)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(character);

            return category == UnicodeCategory.NonSpacingMark || // Mn
                   category == UnicodeCategory.SpacingCombiningMark || // Mc
                   category == UnicodeCategory.ConnectorPunctuation || // Pc
                   category == UnicodeCategory.Format; // Cf
        }

        private static bool IsHexDigit(char value)
        {
            return (value >= '0' && value <= '9') || (value >= 'A' && value <= 'F') || (value >= 'a' && value <= 'f');
        }

        private enum CSharpTokenizerState
        {
            Data,
            BlockComment,
            QuotedCharacterLiteral,
            QuotedStringLiteral,
            VerbatimStringLiteral,

            // Razor Comments - need to be the same for HTML and CSharp
            AfterRazorCommentTransition = RazorCommentTokenizerState.AfterRazorCommentTransition,
            EscapedRazorCommentTransition = RazorCommentTokenizerState.EscapedRazorCommentTransition,
            RazorCommentBody = RazorCommentTokenizerState.RazorCommentBody,
            StarAfterRazorCommentBody = RazorCommentTokenizerState.StarAfterRazorCommentBody,
            AtSymbolAfterRazorCommentBody = RazorCommentTokenizerState.AtSymbolAfterRazorCommentBody,
        }
    }
}
