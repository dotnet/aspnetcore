// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.AspNetCore.Razor.Language.Syntax.InternalSyntax;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    internal class CSharpLanguageCharacteristics : LanguageCharacteristics<CSharpTokenizer>
    {
        private static readonly CSharpLanguageCharacteristics _instance = new CSharpLanguageCharacteristics();

        private static Dictionary<SyntaxKind, string> _tokenSamples = new Dictionary<SyntaxKind, string>()
        {
            { SyntaxKind.Arrow, "->" },
            { SyntaxKind.Minus, "-" },
            { SyntaxKind.Decrement, "--" },
            { SyntaxKind.MinusAssign, "-=" },
            { SyntaxKind.NotEqual, "!=" },
            { SyntaxKind.Not, "!" },
            { SyntaxKind.Modulo, "%" },
            { SyntaxKind.ModuloAssign, "%=" },
            { SyntaxKind.AndAssign, "&=" },
            { SyntaxKind.And, "&" },
            { SyntaxKind.DoubleAnd, "&&" },
            { SyntaxKind.LeftParenthesis, "(" },
            { SyntaxKind.RightParenthesis, ")" },
            { SyntaxKind.Star, "*" },
            { SyntaxKind.MultiplyAssign, "*=" },
            { SyntaxKind.Comma, "," },
            { SyntaxKind.Dot, "." },
            { SyntaxKind.Slash, "/" },
            { SyntaxKind.DivideAssign, "/=" },
            { SyntaxKind.DoubleColon, "::" },
            { SyntaxKind.Colon, ":" },
            { SyntaxKind.Semicolon, ";" },
            { SyntaxKind.QuestionMark, "?" },
            { SyntaxKind.NullCoalesce, "??" },
            { SyntaxKind.RightBracket, "]" },
            { SyntaxKind.LeftBracket, "[" },
            { SyntaxKind.XorAssign, "^=" },
            { SyntaxKind.Xor, "^" },
            { SyntaxKind.LeftBrace, "{" },
            { SyntaxKind.OrAssign, "|=" },
            { SyntaxKind.DoubleOr, "||" },
            { SyntaxKind.Or, "|" },
            { SyntaxKind.RightBrace, "}" },
            { SyntaxKind.Tilde, "~" },
            { SyntaxKind.Plus, "+" },
            { SyntaxKind.PlusAssign, "+=" },
            { SyntaxKind.Increment, "++" },
            { SyntaxKind.LessThan, "<" },
            { SyntaxKind.LessThanEqual, "<=" },
            { SyntaxKind.LeftShift, "<<" },
            { SyntaxKind.LeftShiftAssign, "<<=" },
            { SyntaxKind.Assign, "=" },
            { SyntaxKind.Equals, "==" },
            { SyntaxKind.GreaterThan, ">" },
            { SyntaxKind.GreaterThanEqual, ">=" },
            { SyntaxKind.RightShift, ">>" },
            { SyntaxKind.RightShiftAssign, ">>=" },
            { SyntaxKind.Hash, "#" },
            { SyntaxKind.Transition, "@" },
        };

        protected CSharpLanguageCharacteristics()
        {
        }

        public static CSharpLanguageCharacteristics Instance => _instance;

        public override CSharpTokenizer CreateTokenizer(ITextDocument source)
        {
            return new CSharpTokenizer(source);
        }

        protected override SyntaxToken CreateToken(string content, SyntaxKind kind, IReadOnlyList<RazorDiagnostic> errors)
        {
            return SyntaxFactory.Token(kind, content, errors);
        }

        public override string GetSample(SyntaxKind kind)
        {
            string sample;
            if (!_tokenSamples.TryGetValue(kind, out sample))
            {
                switch (kind)
                {
                    case SyntaxKind.Identifier:
                        return Resources.CSharpToken_Identifier;
                    case SyntaxKind.Keyword:
                        return Resources.CSharpToken_Keyword;
                    case SyntaxKind.IntegerLiteral:
                        return Resources.CSharpToken_IntegerLiteral;
                    case SyntaxKind.NewLine:
                        return Resources.CSharpToken_Newline;
                    case SyntaxKind.Whitespace:
                        return Resources.CSharpToken_Whitespace;
                    case SyntaxKind.CSharpComment:
                        return Resources.CSharpToken_Comment;
                    case SyntaxKind.RealLiteral:
                        return Resources.CSharpToken_RealLiteral;
                    case SyntaxKind.CharacterLiteral:
                        return Resources.CSharpToken_CharacterLiteral;
                    case SyntaxKind.StringLiteral:
                        return Resources.CSharpToken_StringLiteral;
                    default:
                        return Resources.Token_Unknown;
                }
            }
            return sample;
        }

        public override SyntaxToken CreateMarkerToken()
        {
            return SyntaxFactory.Token(SyntaxKind.Unknown, string.Empty);
        }

        public override SyntaxKind GetKnownTokenType(KnownTokenType type)
        {
            switch (type)
            {
                case KnownTokenType.Identifier:
                    return SyntaxKind.Identifier;
                case KnownTokenType.Keyword:
                    return SyntaxKind.Keyword;
                case KnownTokenType.NewLine:
                    return SyntaxKind.NewLine;
                case KnownTokenType.WhiteSpace:
                    return SyntaxKind.Whitespace;
                case KnownTokenType.Transition:
                    return SyntaxKind.Transition;
                case KnownTokenType.CommentStart:
                    return SyntaxKind.RazorCommentTransition;
                case KnownTokenType.CommentStar:
                    return SyntaxKind.RazorCommentStar;
                case KnownTokenType.CommentBody:
                    return SyntaxKind.RazorComment;
                default:
                    return SyntaxKind.Unknown;
            }
        }

        public override SyntaxKind FlipBracket(SyntaxKind bracket)
        {
            switch (bracket)
            {
                case SyntaxKind.LeftBrace:
                    return SyntaxKind.RightBrace;
                case SyntaxKind.LeftBracket:
                    return SyntaxKind.RightBracket;
                case SyntaxKind.LeftParenthesis:
                    return SyntaxKind.RightParenthesis;
                case SyntaxKind.LessThan:
                    return SyntaxKind.GreaterThan;
                case SyntaxKind.RightBrace:
                    return SyntaxKind.LeftBrace;
                case SyntaxKind.RightBracket:
                    return SyntaxKind.LeftBracket;
                case SyntaxKind.RightParenthesis:
                    return SyntaxKind.LeftParenthesis;
                case SyntaxKind.GreaterThan:
                    return SyntaxKind.LessThan;
                default:
                    Debug.Fail("FlipBracket must be called with a bracket character");
                    return SyntaxKind.Unknown;
            }
        }

        public static string GetKeyword(CSharpKeyword keyword)
        {
            return keyword.ToString().ToLowerInvariant();
        }
    }
}