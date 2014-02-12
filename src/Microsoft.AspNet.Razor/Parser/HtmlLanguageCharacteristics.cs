// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.Text;
using Microsoft.AspNet.Razor.Tokenizer;
using Microsoft.AspNet.Razor.Tokenizer.Symbols;

namespace Microsoft.AspNet.Razor.Parser
{
    public class HtmlLanguageCharacteristics : LanguageCharacteristics<HtmlTokenizer, HtmlSymbol, HtmlSymbolType>
    {
        private static readonly HtmlLanguageCharacteristics _instance = new HtmlLanguageCharacteristics();

        private HtmlLanguageCharacteristics()
        {
        }

        public static HtmlLanguageCharacteristics Instance
        {
            get { return _instance; }
        }

        public override string GetSample(HtmlSymbolType type)
        {
            switch (type)
            {
            case HtmlSymbolType.Text:
                return RazorResources.HtmlSymbol_Text;
            case HtmlSymbolType.WhiteSpace:
                return RazorResources.HtmlSymbol_WhiteSpace;
            case HtmlSymbolType.NewLine:
                return RazorResources.HtmlSymbol_NewLine;
            case HtmlSymbolType.OpenAngle:
                return "<";
            case HtmlSymbolType.Bang:
                return "!";
            case HtmlSymbolType.Solidus:
                return "/";
            case HtmlSymbolType.QuestionMark:
                return "?";
            case HtmlSymbolType.DoubleHyphen:
                return "--";
            case HtmlSymbolType.LeftBracket:
                return "[";
            case HtmlSymbolType.CloseAngle:
                return ">";
            case HtmlSymbolType.RightBracket:
                return "]";
            case HtmlSymbolType.Equals:
                return "=";
            case HtmlSymbolType.DoubleQuote:
                return "\"";
            case HtmlSymbolType.SingleQuote:
                return "'";
            case HtmlSymbolType.Transition:
                return "@";
            case HtmlSymbolType.Colon:
                return ":";
            case HtmlSymbolType.RazorComment:
                return RazorResources.HtmlSymbol_RazorComment;
            case HtmlSymbolType.RazorCommentStar:
                return "*";
            case HtmlSymbolType.RazorCommentTransition:
                return "@";
            default:
                return RazorResources.Symbol_Unknown;
            }
        }

        public override HtmlTokenizer CreateTokenizer(ITextDocument source)
        {
            return new HtmlTokenizer(source);
        }

        public override HtmlSymbolType FlipBracket(HtmlSymbolType bracket)
        {
            switch (bracket)
            {
            case HtmlSymbolType.LeftBracket:
                return HtmlSymbolType.RightBracket;
            case HtmlSymbolType.OpenAngle:
                return HtmlSymbolType.CloseAngle;
            case HtmlSymbolType.RightBracket:
                return HtmlSymbolType.LeftBracket;
            case HtmlSymbolType.CloseAngle:
                return HtmlSymbolType.OpenAngle;
            default:
#if NET45
                // No Debug.Fail in CoreCLR

                Debug.Fail("FlipBracket must be called with a bracket character");
#else
                Debug.Assert(false, "FlipBracket must be called with a bracket character");
#endif
                return HtmlSymbolType.Unknown;
            }
        }

        public override HtmlSymbol CreateMarkerSymbol(SourceLocation location)
        {
            return new HtmlSymbol(location, String.Empty, HtmlSymbolType.Unknown);
        }

        public override HtmlSymbolType GetKnownSymbolType(KnownSymbolType type)
        {
            switch (type)
            {
            case KnownSymbolType.CommentStart:
                return HtmlSymbolType.RazorCommentTransition;
            case KnownSymbolType.CommentStar:
                return HtmlSymbolType.RazorCommentStar;
            case KnownSymbolType.CommentBody:
                return HtmlSymbolType.RazorComment;
            case KnownSymbolType.Identifier:
                return HtmlSymbolType.Text;
            case KnownSymbolType.Keyword:
                return HtmlSymbolType.Text;
            case KnownSymbolType.NewLine:
                return HtmlSymbolType.NewLine;
            case KnownSymbolType.Transition:
                return HtmlSymbolType.Transition;
            case KnownSymbolType.WhiteSpace:
                return HtmlSymbolType.WhiteSpace;
            default:
                return HtmlSymbolType.Unknown;
            }
        }

        protected override HtmlSymbol CreateSymbol(SourceLocation location, string content, HtmlSymbolType type, IEnumerable<RazorError> errors)
        {
            return new HtmlSymbol(location, content, type, errors);
        }
    }
}
