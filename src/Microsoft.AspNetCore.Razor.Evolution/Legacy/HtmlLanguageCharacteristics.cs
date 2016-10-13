// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.AspNetCore.Razor.Evolution.Legacy
{
    internal class HtmlLanguageCharacteristics : LanguageCharacteristics<HtmlTokenizer, HtmlSymbol, HtmlSymbolType>
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
                    return LegacyResources.HtmlSymbol_Text;
                case HtmlSymbolType.WhiteSpace:
                    return LegacyResources.HtmlSymbol_WhiteSpace;
                case HtmlSymbolType.NewLine:
                    return LegacyResources.HtmlSymbol_NewLine;
                case HtmlSymbolType.OpenAngle:
                    return "<";
                case HtmlSymbolType.Bang:
                    return "!";
                case HtmlSymbolType.ForwardSlash:
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
                    return LegacyResources.HtmlSymbol_RazorComment;
                case HtmlSymbolType.RazorCommentStar:
                    return "*";
                case HtmlSymbolType.RazorCommentTransition:
                    return "@";
                default:
                    return LegacyResources.Symbol_Unknown;
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
#if NET451
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
            return new HtmlSymbol(location, string.Empty, HtmlSymbolType.Unknown);
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

        protected override HtmlSymbol CreateSymbol(SourceLocation location, string content, HtmlSymbolType type, IReadOnlyList<RazorError> errors)
        {
            return new HtmlSymbol(location, content, type, errors);
        }
    }
}
