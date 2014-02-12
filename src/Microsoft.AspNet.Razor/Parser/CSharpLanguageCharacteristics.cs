// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.Text;
using Microsoft.AspNet.Razor.Tokenizer;
using Microsoft.AspNet.Razor.Tokenizer.Symbols;

namespace Microsoft.AspNet.Razor.Parser
{
    public class CSharpLanguageCharacteristics : LanguageCharacteristics<CSharpTokenizer, CSharpSymbol, CSharpSymbolType>
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
            { CSharpSymbolType.RightShiftAssign, ">>>" },
            { CSharpSymbolType.Hash, "#" },
            { CSharpSymbolType.Transition, "@" },
        };

        private CSharpLanguageCharacteristics()
        {
        }

        public static CSharpLanguageCharacteristics Instance
        {
            get { return _instance; }
        }

        public override CSharpTokenizer CreateTokenizer(ITextDocument source)
        {
            return new CSharpTokenizer(source);
        }

        protected override CSharpSymbol CreateSymbol(SourceLocation location, string content, CSharpSymbolType type, IEnumerable<RazorError> errors)
        {
            return new CSharpSymbol(location, content, type, errors);
        }

        public override string GetSample(CSharpSymbolType type)
        {
            return GetSymbolSample(type);
        }

        public override CSharpSymbol CreateMarkerSymbol(SourceLocation location)
        {
            return new CSharpSymbol(location, String.Empty, CSharpSymbolType.Unknown);
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
#if NET45
                // No Debug.Fail
                Debug.Fail("FlipBracket must be called with a bracket character");
#else
                Debug.Assert(false, "FlipBracket must be called with a bracket character");
#endif
                return CSharpSymbolType.Unknown;
            }
        }

        [SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase", Justification = "C# Keywords are lower-case")]
        public static string GetKeyword(CSharpKeyword keyword)
        {
            return keyword.ToString().ToLowerInvariant();
        }

        public static string GetSymbolSample(CSharpSymbolType type)
        {
            string sample;
            if (!_symbolSamples.TryGetValue(type, out sample))
            {
                switch (type)
                {
                case CSharpSymbolType.Identifier:
                    return RazorResources.CSharpSymbol_Identifier;
                case CSharpSymbolType.Keyword:
                    return RazorResources.CSharpSymbol_Keyword;
                case CSharpSymbolType.IntegerLiteral:
                    return RazorResources.CSharpSymbol_IntegerLiteral;
                case CSharpSymbolType.NewLine:
                    return RazorResources.CSharpSymbol_Newline;
                case CSharpSymbolType.WhiteSpace:
                    return RazorResources.CSharpSymbol_Whitespace;
                case CSharpSymbolType.Comment:
                    return RazorResources.CSharpSymbol_Comment;
                case CSharpSymbolType.RealLiteral:
                    return RazorResources.CSharpSymbol_RealLiteral;
                case CSharpSymbolType.CharacterLiteral:
                    return RazorResources.CSharpSymbol_CharacterLiteral;
                case CSharpSymbolType.StringLiteral:
                    return RazorResources.CSharpSymbol_StringLiteral;
                default:
                    return RazorResources.Symbol_Unknown;
                }
            }
            return sample;
        }
    }
}
