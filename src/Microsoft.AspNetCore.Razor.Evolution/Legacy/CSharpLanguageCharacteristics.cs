// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.AspNetCore.Razor.Evolution.Legacy
{
    internal class CSharpLanguageCharacteristics : LanguageCharacteristics<CSharpTokenizer, CSharpSymbol, CSharpSymbolType>
    {
        private static readonly CSharpLanguageCharacteristics _instance = new CSharpLanguageCharacteristics();

        private static Dictionary<CSharpSymbolType, string> _symbolSamples = new Dictionary<CSharpSymbolType, string>()
        {
            { CSharpSymbolType.Arrow, "->" },
            { CSharpSymbolType.Minus, "-" },
            { CSharpSymbolType.Decrement, "--" },
            { CSharpSymbolType.MinusAssign, "-=" },
            { CSharpSymbolType.NotEqual, "!=" },
            { CSharpSymbolType.Not, "!" },
            { CSharpSymbolType.Modulo, "%" },
            { CSharpSymbolType.ModuloAssign, "%=" },
            { CSharpSymbolType.AndAssign, "&=" },
            { CSharpSymbolType.And, "&" },
            { CSharpSymbolType.DoubleAnd, "&&" },
            { CSharpSymbolType.LeftParenthesis, "(" },
            { CSharpSymbolType.RightParenthesis, ")" },
            { CSharpSymbolType.Star, "*" },
            { CSharpSymbolType.MultiplyAssign, "*=" },
            { CSharpSymbolType.Comma, "," },
            { CSharpSymbolType.Dot, "." },
            { CSharpSymbolType.Slash, "/" },
            { CSharpSymbolType.DivideAssign, "/=" },
            { CSharpSymbolType.DoubleColon, "::" },
            { CSharpSymbolType.Colon, ":" },
            { CSharpSymbolType.Semicolon, ";" },
            { CSharpSymbolType.QuestionMark, "?" },
            { CSharpSymbolType.NullCoalesce, "??" },
            { CSharpSymbolType.RightBracket, "]" },
            { CSharpSymbolType.LeftBracket, "[" },
            { CSharpSymbolType.XorAssign, "^=" },
            { CSharpSymbolType.Xor, "^" },
            { CSharpSymbolType.LeftBrace, "{" },
            { CSharpSymbolType.OrAssign, "|=" },
            { CSharpSymbolType.DoubleOr, "||" },
            { CSharpSymbolType.Or, "|" },
            { CSharpSymbolType.RightBrace, "}" },
            { CSharpSymbolType.Tilde, "~" },
            { CSharpSymbolType.Plus, "+" },
            { CSharpSymbolType.PlusAssign, "+=" },
            { CSharpSymbolType.Increment, "++" },
            { CSharpSymbolType.LessThan, "<" },
            { CSharpSymbolType.LessThanEqual, "<=" },
            { CSharpSymbolType.LeftShift, "<<" },
            { CSharpSymbolType.LeftShiftAssign, "<<=" },
            { CSharpSymbolType.Assign, "=" },
            { CSharpSymbolType.Equals, "==" },
            { CSharpSymbolType.GreaterThan, ">" },
            { CSharpSymbolType.GreaterThanEqual, ">=" },
            { CSharpSymbolType.RightShift, ">>" },
            { CSharpSymbolType.RightShiftAssign, ">>=" },
            { CSharpSymbolType.Hash, "#" },
            { CSharpSymbolType.Transition, "@" },
        };

        private CSharpLanguageCharacteristics()
        {
        }

        public static CSharpLanguageCharacteristics Instance => _instance;

        public override CSharpTokenizer CreateTokenizer(ITextDocument source)
        {
            return new CSharpTokenizer(source);
        }

        protected override CSharpSymbol CreateSymbol(string content, CSharpSymbolType type, IReadOnlyList<RazorError> errors)
        {
            return new CSharpSymbol(content, type, errors);
        }

        public override string GetSample(CSharpSymbolType type)
        {
            string sample;
            if (!_symbolSamples.TryGetValue(type, out sample))
            {
                switch (type)
                {
                    case CSharpSymbolType.Identifier:
                        return LegacyResources.CSharpSymbol_Identifier;
                    case CSharpSymbolType.Keyword:
                        return LegacyResources.CSharpSymbol_Keyword;
                    case CSharpSymbolType.IntegerLiteral:
                        return LegacyResources.CSharpSymbol_IntegerLiteral;
                    case CSharpSymbolType.NewLine:
                        return LegacyResources.CSharpSymbol_Newline;
                    case CSharpSymbolType.WhiteSpace:
                        return LegacyResources.CSharpSymbol_Whitespace;
                    case CSharpSymbolType.Comment:
                        return LegacyResources.CSharpSymbol_Comment;
                    case CSharpSymbolType.RealLiteral:
                        return LegacyResources.CSharpSymbol_RealLiteral;
                    case CSharpSymbolType.CharacterLiteral:
                        return LegacyResources.CSharpSymbol_CharacterLiteral;
                    case CSharpSymbolType.StringLiteral:
                        return LegacyResources.CSharpSymbol_StringLiteral;
                    default:
                        return LegacyResources.Symbol_Unknown;
                }
            }
            return sample;
        }

        public override CSharpSymbol CreateMarkerSymbol()
        {
            return new CSharpSymbol(string.Empty, CSharpSymbolType.Unknown);
        }

        public override CSharpSymbolType GetKnownSymbolType(KnownSymbolType type)
        {
            switch (type)
            {
                case KnownSymbolType.Identifier:
                    return CSharpSymbolType.Identifier;
                case KnownSymbolType.Keyword:
                    return CSharpSymbolType.Keyword;
                case KnownSymbolType.NewLine:
                    return CSharpSymbolType.NewLine;
                case KnownSymbolType.WhiteSpace:
                    return CSharpSymbolType.WhiteSpace;
                case KnownSymbolType.Transition:
                    return CSharpSymbolType.Transition;
                case KnownSymbolType.CommentStart:
                    return CSharpSymbolType.RazorCommentTransition;
                case KnownSymbolType.CommentStar:
                    return CSharpSymbolType.RazorCommentStar;
                case KnownSymbolType.CommentBody:
                    return CSharpSymbolType.RazorComment;
                default:
                    return CSharpSymbolType.Unknown;
            }
        }

        public override CSharpSymbolType FlipBracket(CSharpSymbolType bracket)
        {
            switch (bracket)
            {
                case CSharpSymbolType.LeftBrace:
                    return CSharpSymbolType.RightBrace;
                case CSharpSymbolType.LeftBracket:
                    return CSharpSymbolType.RightBracket;
                case CSharpSymbolType.LeftParenthesis:
                    return CSharpSymbolType.RightParenthesis;
                case CSharpSymbolType.LessThan:
                    return CSharpSymbolType.GreaterThan;
                case CSharpSymbolType.RightBrace:
                    return CSharpSymbolType.LeftBrace;
                case CSharpSymbolType.RightBracket:
                    return CSharpSymbolType.LeftBracket;
                case CSharpSymbolType.RightParenthesis:
                    return CSharpSymbolType.LeftParenthesis;
                case CSharpSymbolType.GreaterThan:
                    return CSharpSymbolType.LessThan;
                default:
                    Debug.Fail("FlipBracket must be called with a bracket character");
                    return CSharpSymbolType.Unknown;
            }
        }

        public static string GetKeyword(CSharpKeyword keyword)
        {
            return keyword.ToString().ToLowerInvariant();
        }
    }
}
