// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    internal class CSharpLanguageCharacteristics : LanguageCharacteristics<CSharpTokenizer, CSharpToken, CSharpTokenType>
    {
        private static readonly CSharpLanguageCharacteristics _instance = new CSharpLanguageCharacteristics();

        private static Dictionary<CSharpTokenType, string> _tokenSamples = new Dictionary<CSharpTokenType, string>()
        {
            { CSharpTokenType.Arrow, "->" },
            { CSharpTokenType.Minus, "-" },
            { CSharpTokenType.Decrement, "--" },
            { CSharpTokenType.MinusAssign, "-=" },
            { CSharpTokenType.NotEqual, "!=" },
            { CSharpTokenType.Not, "!" },
            { CSharpTokenType.Modulo, "%" },
            { CSharpTokenType.ModuloAssign, "%=" },
            { CSharpTokenType.AndAssign, "&=" },
            { CSharpTokenType.And, "&" },
            { CSharpTokenType.DoubleAnd, "&&" },
            { CSharpTokenType.LeftParenthesis, "(" },
            { CSharpTokenType.RightParenthesis, ")" },
            { CSharpTokenType.Star, "*" },
            { CSharpTokenType.MultiplyAssign, "*=" },
            { CSharpTokenType.Comma, "," },
            { CSharpTokenType.Dot, "." },
            { CSharpTokenType.Slash, "/" },
            { CSharpTokenType.DivideAssign, "/=" },
            { CSharpTokenType.DoubleColon, "::" },
            { CSharpTokenType.Colon, ":" },
            { CSharpTokenType.Semicolon, ";" },
            { CSharpTokenType.QuestionMark, "?" },
            { CSharpTokenType.NullCoalesce, "??" },
            { CSharpTokenType.RightBracket, "]" },
            { CSharpTokenType.LeftBracket, "[" },
            { CSharpTokenType.XorAssign, "^=" },
            { CSharpTokenType.Xor, "^" },
            { CSharpTokenType.LeftBrace, "{" },
            { CSharpTokenType.OrAssign, "|=" },
            { CSharpTokenType.DoubleOr, "||" },
            { CSharpTokenType.Or, "|" },
            { CSharpTokenType.RightBrace, "}" },
            { CSharpTokenType.Tilde, "~" },
            { CSharpTokenType.Plus, "+" },
            { CSharpTokenType.PlusAssign, "+=" },
            { CSharpTokenType.Increment, "++" },
            { CSharpTokenType.LessThan, "<" },
            { CSharpTokenType.LessThanEqual, "<=" },
            { CSharpTokenType.LeftShift, "<<" },
            { CSharpTokenType.LeftShiftAssign, "<<=" },
            { CSharpTokenType.Assign, "=" },
            { CSharpTokenType.Equals, "==" },
            { CSharpTokenType.GreaterThan, ">" },
            { CSharpTokenType.GreaterThanEqual, ">=" },
            { CSharpTokenType.RightShift, ">>" },
            { CSharpTokenType.RightShiftAssign, ">>=" },
            { CSharpTokenType.Hash, "#" },
            { CSharpTokenType.Transition, "@" },
        };

        protected CSharpLanguageCharacteristics()
        {
        }

        public static CSharpLanguageCharacteristics Instance => _instance;

        public override CSharpTokenizer CreateTokenizer(ITextDocument source)
        {
            return new CSharpTokenizer(source);
        }

        protected override CSharpToken CreateToken(string content, CSharpTokenType type, IReadOnlyList<RazorDiagnostic> errors)
        {
            return new CSharpToken(content, type, errors);
        }

        public override string GetSample(CSharpTokenType type)
        {
            string sample;
            if (!_tokenSamples.TryGetValue(type, out sample))
            {
                switch (type)
                {
                    case CSharpTokenType.Identifier:
                        return Resources.CSharpToken_Identifier;
                    case CSharpTokenType.Keyword:
                        return Resources.CSharpToken_Keyword;
                    case CSharpTokenType.IntegerLiteral:
                        return Resources.CSharpToken_IntegerLiteral;
                    case CSharpTokenType.NewLine:
                        return Resources.CSharpToken_Newline;
                    case CSharpTokenType.WhiteSpace:
                        return Resources.CSharpToken_Whitespace;
                    case CSharpTokenType.Comment:
                        return Resources.CSharpToken_Comment;
                    case CSharpTokenType.RealLiteral:
                        return Resources.CSharpToken_RealLiteral;
                    case CSharpTokenType.CharacterLiteral:
                        return Resources.CSharpToken_CharacterLiteral;
                    case CSharpTokenType.StringLiteral:
                        return Resources.CSharpToken_StringLiteral;
                    default:
                        return Resources.Token_Unknown;
                }
            }
            return sample;
        }

        public override CSharpToken CreateMarkerToken()
        {
            return new CSharpToken(string.Empty, CSharpTokenType.Unknown);
        }

        public override CSharpTokenType GetKnownTokenType(KnownTokenType type)
        {
            switch (type)
            {
                case KnownTokenType.Identifier:
                    return CSharpTokenType.Identifier;
                case KnownTokenType.Keyword:
                    return CSharpTokenType.Keyword;
                case KnownTokenType.NewLine:
                    return CSharpTokenType.NewLine;
                case KnownTokenType.WhiteSpace:
                    return CSharpTokenType.WhiteSpace;
                case KnownTokenType.Transition:
                    return CSharpTokenType.Transition;
                case KnownTokenType.CommentStart:
                    return CSharpTokenType.RazorCommentTransition;
                case KnownTokenType.CommentStar:
                    return CSharpTokenType.RazorCommentStar;
                case KnownTokenType.CommentBody:
                    return CSharpTokenType.RazorComment;
                default:
                    return CSharpTokenType.Unknown;
            }
        }

        public override CSharpTokenType FlipBracket(CSharpTokenType bracket)
        {
            switch (bracket)
            {
                case CSharpTokenType.LeftBrace:
                    return CSharpTokenType.RightBrace;
                case CSharpTokenType.LeftBracket:
                    return CSharpTokenType.RightBracket;
                case CSharpTokenType.LeftParenthesis:
                    return CSharpTokenType.RightParenthesis;
                case CSharpTokenType.LessThan:
                    return CSharpTokenType.GreaterThan;
                case CSharpTokenType.RightBrace:
                    return CSharpTokenType.LeftBrace;
                case CSharpTokenType.RightBracket:
                    return CSharpTokenType.LeftBracket;
                case CSharpTokenType.RightParenthesis:
                    return CSharpTokenType.LeftParenthesis;
                case CSharpTokenType.GreaterThan:
                    return CSharpTokenType.LessThan;
                default:
                    Debug.Fail("FlipBracket must be called with a bracket character");
                    return CSharpTokenType.Unknown;
            }
        }

        public static string GetKeyword(CSharpKeyword keyword)
        {
            return keyword.ToString().ToLowerInvariant();
        }
    }
}