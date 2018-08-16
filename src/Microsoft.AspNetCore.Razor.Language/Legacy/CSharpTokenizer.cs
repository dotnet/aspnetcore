// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using Microsoft.AspNetCore.Razor.Language.Syntax.InternalSyntax;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    internal class CSharpTokenizer : Tokenizer
    {
        private Dictionary<char, Func<SyntaxKind>> _operatorHandlers;

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

            _operatorHandlers = new Dictionary<char, Func<SyntaxKind>>()
            {
                { '-', MinusOperator },
                { '<', LessThanOperator },
                { '>', GreaterThanOperator },
                { '&', CreateTwoCharOperatorHandler(SyntaxKind.And, '=', SyntaxKind.AndAssign, '&', SyntaxKind.DoubleAnd) },
                { '|', CreateTwoCharOperatorHandler(SyntaxKind.Or, '=', SyntaxKind.OrAssign, '|', SyntaxKind.DoubleOr) },
                { '+', CreateTwoCharOperatorHandler(SyntaxKind.Plus, '=', SyntaxKind.PlusAssign, '+', SyntaxKind.Increment) },
                { '=', CreateTwoCharOperatorHandler(SyntaxKind.Assign, '=', SyntaxKind.Equals, '>', SyntaxKind.GreaterThanEqual) },
                { '!', CreateTwoCharOperatorHandler(SyntaxKind.Not, '=', SyntaxKind.NotEqual) },
                { '%', CreateTwoCharOperatorHandler(SyntaxKind.Modulo, '=', SyntaxKind.ModuloAssign) },
                { '*', CreateTwoCharOperatorHandler(SyntaxKind.Star, '=', SyntaxKind.MultiplyAssign) },
                { ':', CreateTwoCharOperatorHandler(SyntaxKind.Colon, ':', SyntaxKind.DoubleColon) },
                { '?', CreateTwoCharOperatorHandler(SyntaxKind.QuestionMark, '?', SyntaxKind.NullCoalesce) },
                { '^', CreateTwoCharOperatorHandler(SyntaxKind.Xor, '=', SyntaxKind.XorAssign) },
                { '(', () => SyntaxKind.LeftParenthesis },
                { ')', () => SyntaxKind.RightParenthesis },
                { '{', () => SyntaxKind.LeftBrace },
                { '}', () => SyntaxKind.RightBrace },
                { '[', () => SyntaxKind.LeftBracket },
                { ']', () => SyntaxKind.RightBracket },
                { ',', () => SyntaxKind.Comma },
                { ';', () => SyntaxKind.Semicolon },
                { '~', () => SyntaxKind.Tilde },
                { '#', () => SyntaxKind.Hash }
            };
        }

        protected override int StartState => (int)CSharpTokenizerState.Data;

        private new CSharpTokenizerState? CurrentState => (CSharpTokenizerState?)base.CurrentState;

        public override SyntaxKind RazorCommentKind => SyntaxKind.RazorComment;

        public override SyntaxKind RazorCommentTransitionKind => SyntaxKind.RazorCommentTransition;

        public override SyntaxKind RazorCommentStarKind => SyntaxKind.RazorCommentStar;

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
                case CSharpTokenizerState.AtTokenAfterRazorCommentBody:
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
                    case SyntaxKind.IntegerLiteral:
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
                    case SyntaxKind.NewLine:
                        if (Buffer[0] == '\n')
                        {
                            return "\n";
                        }
                        break;
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
                    case SyntaxKind.Minus:
                        return "-";
                    case SyntaxKind.Not:
                        return "!";
                    case SyntaxKind.Modulo:
                        return "%";
                    case SyntaxKind.And:
                        return "&";
                    case SyntaxKind.LeftParenthesis:
                        return "(";
                    case SyntaxKind.RightParenthesis:
                        return ")";
                    case SyntaxKind.Star:
                        return "*";
                    case SyntaxKind.Comma:
                        return ",";
                    case SyntaxKind.Dot:
                        return ".";
                    case SyntaxKind.Slash:
                        return "/";
                    case SyntaxKind.Colon:
                        return ":";
                    case SyntaxKind.Semicolon:
                        return ";";
                    case SyntaxKind.QuestionMark:
                        return "?";
                    case SyntaxKind.RightBracket:
                        return "]";
                    case SyntaxKind.LeftBracket:
                        return "[";
                    case SyntaxKind.Xor:
                        return "^";
                    case SyntaxKind.LeftBrace:
                        return "{";
                    case SyntaxKind.Or:
                        return "|";
                    case SyntaxKind.RightBrace:
                        return "}";
                    case SyntaxKind.Tilde:
                        return "~";
                    case SyntaxKind.Plus:
                        return "+";
                    case SyntaxKind.LessThan:
                        return "<";
                    case SyntaxKind.Assign:
                        return "=";
                    case SyntaxKind.GreaterThan:
                        return ">";
                    case SyntaxKind.Hash:
                        return "#";
                    case SyntaxKind.Transition:
                        return "@";

                }
            }
            else if (tokenLength == 2)
            {
                switch (type)
                {
                    case SyntaxKind.NewLine:
                        return "\r\n";
                    case SyntaxKind.Arrow:
                        return "->";
                    case SyntaxKind.Decrement:
                        return "--";
                    case SyntaxKind.MinusAssign:
                        return "-=";
                    case SyntaxKind.NotEqual:
                        return "!=";
                    case SyntaxKind.ModuloAssign:
                        return "%=";
                    case SyntaxKind.AndAssign:
                        return "&=";
                    case SyntaxKind.DoubleAnd:
                        return "&&";
                    case SyntaxKind.MultiplyAssign:
                        return "*=";
                    case SyntaxKind.DivideAssign:
                        return "/=";
                    case SyntaxKind.DoubleColon:
                        return "::";
                    case SyntaxKind.NullCoalesce:
                        return "??";
                    case SyntaxKind.XorAssign:
                        return "^=";
                    case SyntaxKind.OrAssign:
                        return "|=";
                    case SyntaxKind.DoubleOr:
                        return "||";
                    case SyntaxKind.PlusAssign:
                        return "+=";
                    case SyntaxKind.Increment:
                        return "++";
                    case SyntaxKind.LessThanEqual:
                        return "<=";
                    case SyntaxKind.LeftShift:
                        return "<<";
                    case SyntaxKind.Equals:
                        return "==";
                    case SyntaxKind.GreaterThanEqual:
                        if (Buffer[0] == '=')
                        {
                            return "=>";
                        }
                        return ">=";
                    case SyntaxKind.RightShift:
                        return ">>";


                }
            }
            else if (tokenLength == 3)
            {
                switch (type)
                {
                    case SyntaxKind.LeftShiftAssign:
                        return "<<=";
                    case SyntaxKind.RightShiftAssign:
                        return ">>=";
                }
            }

            return base.GetTokenContent(type);
        }

        protected override SyntaxToken CreateToken(string content, SyntaxKind kind, IReadOnlyList<RazorDiagnostic> errors)
        {
            return SyntaxFactory.Token(kind, content, errors);
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
                return Stay(EndToken(SyntaxKind.NewLine));
            }
            else if (ParserHelpers.IsWhitespace(CurrentCharacter))
            {
                // CSharp Spec §2.3.3
                TakeUntil(c => !ParserHelpers.IsWhitespace(c));
                return Stay(EndToken(SyntaxKind.Whitespace));
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
                    return AtToken();
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
                    return Stay(Single(SyntaxKind.Dot));
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
                        return Stay(EndToken(SyntaxKind.DivideAssign));
                    }
                    else
                    {
                        return Stay(EndToken(SyntaxKind.Slash));
                    }
                default:
                    return Stay(EndToken(Operator()));
            }
        }

        private StateResult AtToken()
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
                    EndToken(SyntaxKind.RazorCommentTransition));
            }
            else if (CurrentCharacter == '@')
            {
                // Could be escaped comment transition
                return Transition(
                    CSharpTokenizerState.EscapedRazorCommentTransition,
                    EndToken(SyntaxKind.Transition));
            }

            return Stay(EndToken(SyntaxKind.Transition));
        }

        private StateResult EscapedRazorCommentTransition()
        {
            TakeCurrent();
            return Transition(CSharpTokenizerState.Data, EndToken(SyntaxKind.Transition));
        }

        private SyntaxKind Operator()
        {
            var first = CurrentCharacter;
            TakeCurrent();
            Func<SyntaxKind> handler;
            if (_operatorHandlers.TryGetValue(first, out handler))
            {
                return handler();
            }
            return SyntaxKind.Unknown;
        }

        private SyntaxKind LessThanOperator()
        {
            if (CurrentCharacter == '=')
            {
                TakeCurrent();
                return SyntaxKind.LessThanEqual;
            }
            return SyntaxKind.LessThan;
        }

        private SyntaxKind GreaterThanOperator()
        {
            if (CurrentCharacter == '=')
            {
                TakeCurrent();
                return SyntaxKind.GreaterThanEqual;
            }
            return SyntaxKind.GreaterThan;
        }

        private SyntaxKind MinusOperator()
        {
            if (CurrentCharacter == '>')
            {
                TakeCurrent();
                return SyntaxKind.Arrow;
            }
            else if (CurrentCharacter == '-')
            {
                TakeCurrent();
                return SyntaxKind.Decrement;
            }
            else if (CurrentCharacter == '=')
            {
                TakeCurrent();
                return SyntaxKind.MinusAssign;
            }
            return SyntaxKind.Minus;
        }

        private Func<SyntaxKind> CreateTwoCharOperatorHandler(SyntaxKind typeIfOnlyFirst, char second, SyntaxKind typeIfBoth)
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

        private Func<SyntaxKind> CreateTwoCharOperatorHandler(SyntaxKind typeIfOnlyFirst, char option1, SyntaxKind typeIfOption1, char option2, SyntaxKind typeIfOption2)
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
                    RazorDiagnosticFactory.CreateParsing_UnterminatedStringLiteral(
                        new SourceSpan(CurrentStart, contentLength: 1 /* end of file */)));
            }
            return Transition(CSharpTokenizerState.Data, EndToken(SyntaxKind.StringLiteral));
        }

        private StateResult QuotedCharacterLiteral() => QuotedLiteral('\'', SyntaxKind.CharacterLiteral);

        private StateResult QuotedStringLiteral() => QuotedLiteral('\"', SyntaxKind.StringLiteral);

        private StateResult QuotedLiteral(char quote, SyntaxKind literalType)
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
                    RazorDiagnosticFactory.CreateParsing_UnterminatedStringLiteral(
                        new SourceSpan(CurrentStart, contentLength: 1 /* " */)));
            }
            else
            {
                TakeCurrent(); // No-op if at EOF
            }
            return Transition(CSharpTokenizerState.Data, EndToken(literalType));
        }

        // CSharp Spec §2.3.2
        private StateResult BlockComment()
        {
            TakeUntil(c => c == '*');
            if (EndOfFile)
            {
                CurrentErrors.Add(
                    RazorDiagnosticFactory.CreateParsing_BlockCommentNotTerminated(
                        new SourceSpan(CurrentStart, contentLength: 1 /* end of file */)));
                    
                return Transition(CSharpTokenizerState.Data, EndToken(SyntaxKind.CSharpComment));
            }
            if (CurrentCharacter == '*')
            {
                TakeCurrent();
                if (CurrentCharacter == '/')
                {
                    TakeCurrent();
                    return Transition(CSharpTokenizerState.Data, EndToken(SyntaxKind.CSharpComment));
                }
            }
            return Stay();
        }

        // CSharp Spec §2.3.2
        private StateResult SingleLineComment()
        {
            TakeUntil(c => ParserHelpers.IsNewLine(c));
            return Stay(EndToken(SyntaxKind.CSharpComment));
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
            return Stay(EndToken(SyntaxKind.IntegerLiteral));
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
                return Stay(EndToken(SyntaxKind.IntegerLiteral));
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
            return Stay(EndToken(SyntaxKind.RealLiteral));
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
            SyntaxToken token = null;
            if (HaveContent)
            {
                CSharpKeyword keyword;
                var type = SyntaxKind.Identifier;
                var tokenContent = Buffer.ToString();
                if (_keywords.TryGetValue(tokenContent, out keyword))
                {
                    type = SyntaxKind.Keyword;
                }

                token = SyntaxFactory.Token(type, tokenContent);
                
                Buffer.Clear();
                CurrentErrors.Clear();
            }

            return Stay(token);
        }

        private StateResult Transition(CSharpTokenizerState state)
        {
            return Transition((int)state, result: null);
        }

        private StateResult Transition(CSharpTokenizerState state, SyntaxToken result)
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

        internal static CSharpKeyword? GetTokenKeyword(SyntaxToken token)
        {
            if (token != null && _keywords.TryGetValue(token.Content, out var keyword))
            {
                return keyword;
            }

            return null;
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
            AtTokenAfterRazorCommentBody = RazorCommentTokenizerState.AtTokenAfterRazorCommentBody,
        }
    }
}
