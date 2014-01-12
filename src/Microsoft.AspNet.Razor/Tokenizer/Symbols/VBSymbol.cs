// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.Resources;
using Microsoft.AspNet.Razor.Text;

namespace Microsoft.AspNet.Razor.Tokenizer.Symbols
{
    public class VBSymbol : SymbolBase<VBSymbolType>
    {
        // Helper constructor
        private static Dictionary<VBSymbolType, string> _symbolSamples = new Dictionary<VBSymbolType, string>()
        {
            { VBSymbolType.LineContinuation, "_" },
            { VBSymbolType.LeftParenthesis, "(" },
            { VBSymbolType.RightParenthesis, ")" },
            { VBSymbolType.LeftBracket, "[" },
            { VBSymbolType.RightBracket, "]" },
            { VBSymbolType.LeftBrace, "{" },
            { VBSymbolType.RightBrace, "}" },
            { VBSymbolType.Bang, "!" },
            { VBSymbolType.Hash, "#" },
            { VBSymbolType.Comma, "," },
            { VBSymbolType.Dot, "." },
            { VBSymbolType.Colon, ":" },
            { VBSymbolType.QuestionMark, "?" },
            { VBSymbolType.Concatenation, "&" },
            { VBSymbolType.Multiply, "*" },
            { VBSymbolType.Add, "+" },
            { VBSymbolType.Subtract, "-" },
            { VBSymbolType.Divide, "/" },
            { VBSymbolType.IntegerDivide, "\\" },
            { VBSymbolType.Exponentiation, "^" },
            { VBSymbolType.Equal, "=" },
            { VBSymbolType.LessThan, "<" },
            { VBSymbolType.GreaterThan, ">" },
            { VBSymbolType.Dollar, "$" },
            { VBSymbolType.Transition, "@" },
            { VBSymbolType.RazorCommentTransition, "@" },
            { VBSymbolType.RazorCommentStar, "*" }
        };

        public VBSymbol(int offset, int line, int column, string content, VBSymbolType type)
            : this(new SourceLocation(offset, line, column), content, type, Enumerable.Empty<RazorError>())
        {
        }

        public VBSymbol(SourceLocation start, string content, VBSymbolType type)
            : this(start, content, type, Enumerable.Empty<RazorError>())
        {
        }

        public VBSymbol(int offset, int line, int column, string content, VBSymbolType type, IEnumerable<RazorError> errors)
            : base(new SourceLocation(offset, line, column), content, type, errors)
        {
        }

        public VBSymbol(SourceLocation start, string content, VBSymbolType type, IEnumerable<RazorError> errors)
            : base(start, content, type, errors)
        {
        }

        public VBKeyword? Keyword { get; set; }

        public override bool Equals(object obj)
        {
            VBSymbol other = obj as VBSymbol;
            return base.Equals(obj) && other.Keyword == Keyword;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode() ^ Keyword.GetHashCode();
        }

        public static string GetSample(VBSymbolType type)
        {
            string sample;
            if (!_symbolSamples.TryGetValue(type, out sample))
            {
                switch (type)
                {
                    case VBSymbolType.WhiteSpace:
                        return RazorResources.VBSymbol_WhiteSpace;
                    case VBSymbolType.NewLine:
                        return RazorResources.VBSymbol_NewLine;
                    case VBSymbolType.Comment:
                        return RazorResources.VBSymbol_Comment;
                    case VBSymbolType.Identifier:
                        return RazorResources.VBSymbol_Identifier;
                    case VBSymbolType.Keyword:
                        return RazorResources.VBSymbol_Keyword;
                    case VBSymbolType.IntegerLiteral:
                        return RazorResources.VBSymbol_IntegerLiteral;
                    case VBSymbolType.FloatingPointLiteral:
                        return RazorResources.VBSymbol_FloatingPointLiteral;
                    case VBSymbolType.StringLiteral:
                        return RazorResources.VBSymbol_StringLiteral;
                    case VBSymbolType.CharacterLiteral:
                        return RazorResources.VBSymbol_CharacterLiteral;
                    case VBSymbolType.DateLiteral:
                        return RazorResources.VBSymbol_DateLiteral;
                    case VBSymbolType.RazorComment:
                        return RazorResources.VBSymbol_RazorComment;
                    default:
                        return RazorResources.Symbol_Unknown;
                }
            }
            return sample;
        }
    }
}
