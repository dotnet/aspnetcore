// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    internal class HtmlLanguageCharacteristics : LanguageCharacteristics<HtmlTokenizer, HtmlToken, HtmlTokenType>
    {
        private static readonly HtmlLanguageCharacteristics _instance = new HtmlLanguageCharacteristics();

        protected HtmlLanguageCharacteristics()
        {
        }

        public static HtmlLanguageCharacteristics Instance
        {
            get { return _instance; }
        }

        public override string GetSample(HtmlTokenType type)
        {
            switch (type)
            {
                case HtmlTokenType.Text:
                    return Resources.HtmlToken_Text;
                case HtmlTokenType.WhiteSpace:
                    return Resources.HtmlToken_WhiteSpace;
                case HtmlTokenType.NewLine:
                    return Resources.HtmlToken_NewLine;
                case HtmlTokenType.OpenAngle:
                    return "<";
                case HtmlTokenType.Bang:
                    return "!";
                case HtmlTokenType.ForwardSlash:
                    return "/";
                case HtmlTokenType.QuestionMark:
                    return "?";
                case HtmlTokenType.DoubleHyphen:
                    return "--";
                case HtmlTokenType.LeftBracket:
                    return "[";
                case HtmlTokenType.CloseAngle:
                    return ">";
                case HtmlTokenType.RightBracket:
                    return "]";
                case HtmlTokenType.Equals:
                    return "=";
                case HtmlTokenType.DoubleQuote:
                    return "\"";
                case HtmlTokenType.SingleQuote:
                    return "'";
                case HtmlTokenType.Transition:
                    return "@";
                case HtmlTokenType.Colon:
                    return ":";
                case HtmlTokenType.RazorComment:
                    return Resources.HtmlToken_RazorComment;
                case HtmlTokenType.RazorCommentStar:
                    return "*";
                case HtmlTokenType.RazorCommentTransition:
                    return "@";
                default:
                    return Resources.Token_Unknown;
            }
        }

        public override HtmlTokenizer CreateTokenizer(ITextDocument source)
        {
            return new HtmlTokenizer(source);
        }

        public override HtmlTokenType FlipBracket(HtmlTokenType bracket)
        {
            switch (bracket)
            {
                case HtmlTokenType.LeftBracket:
                    return HtmlTokenType.RightBracket;
                case HtmlTokenType.OpenAngle:
                    return HtmlTokenType.CloseAngle;
                case HtmlTokenType.RightBracket:
                    return HtmlTokenType.LeftBracket;
                case HtmlTokenType.CloseAngle:
                    return HtmlTokenType.OpenAngle;
                default:
                    Debug.Fail("FlipBracket must be called with a bracket character");
                    return HtmlTokenType.Unknown;
            }
        }

        public override HtmlToken CreateMarkerToken()
        {
            return new HtmlToken(string.Empty, HtmlTokenType.Unknown);
        }

        public override HtmlTokenType GetKnownTokenType(KnownTokenType type)
        {
            switch (type)
            {
                case KnownTokenType.CommentStart:
                    return HtmlTokenType.RazorCommentTransition;
                case KnownTokenType.CommentStar:
                    return HtmlTokenType.RazorCommentStar;
                case KnownTokenType.CommentBody:
                    return HtmlTokenType.RazorComment;
                case KnownTokenType.Identifier:
                    return HtmlTokenType.Text;
                case KnownTokenType.Keyword:
                    return HtmlTokenType.Text;
                case KnownTokenType.NewLine:
                    return HtmlTokenType.NewLine;
                case KnownTokenType.Transition:
                    return HtmlTokenType.Transition;
                case KnownTokenType.WhiteSpace:
                    return HtmlTokenType.WhiteSpace;
                default:
                    return HtmlTokenType.Unknown;
            }
        }

        protected override HtmlToken CreateToken(string content, HtmlTokenType type, IReadOnlyList<RazorDiagnostic> errors)
        {
            return new HtmlToken(content, type, errors);
        }
    }
}
