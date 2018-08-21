// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.AspNetCore.Razor.Language.Syntax.InternalSyntax;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    internal class HtmlLanguageCharacteristics : LanguageCharacteristics<HtmlTokenizer>
    {
        private static readonly HtmlLanguageCharacteristics _instance = new HtmlLanguageCharacteristics();

        protected HtmlLanguageCharacteristics()
        {
        }

        public static HtmlLanguageCharacteristics Instance
        {
            get { return _instance; }
        }

        public override string GetSample(SyntaxKind type)
        {
            switch (type)
            {
                case SyntaxKind.HtmlTextLiteral:
                    return Resources.HtmlToken_Text;
                case SyntaxKind.Whitespace:
                    return Resources.HtmlToken_WhiteSpace;
                case SyntaxKind.NewLine:
                    return Resources.HtmlToken_NewLine;
                case SyntaxKind.OpenAngle:
                    return "<";
                case SyntaxKind.Bang:
                    return "!";
                case SyntaxKind.ForwardSlash:
                    return "/";
                case SyntaxKind.QuestionMark:
                    return "?";
                case SyntaxKind.DoubleHyphen:
                    return "--";
                case SyntaxKind.LeftBracket:
                    return "[";
                case SyntaxKind.CloseAngle:
                    return ">";
                case SyntaxKind.RightBracket:
                    return "]";
                case SyntaxKind.Equals:
                    return "=";
                case SyntaxKind.DoubleQuote:
                    return "\"";
                case SyntaxKind.SingleQuote:
                    return "'";
                case SyntaxKind.Transition:
                    return "@";
                case SyntaxKind.Colon:
                    return ":";
                case SyntaxKind.RazorCommentLiteral:
                    return Resources.HtmlToken_RazorComment;
                case SyntaxKind.RazorCommentStar:
                    return "*";
                case SyntaxKind.RazorCommentTransition:
                    return "@";
                default:
                    return Resources.Token_Unknown;
            }
        }

        public override HtmlTokenizer CreateTokenizer(ITextDocument source)
        {
            return new HtmlTokenizer(source);
        }

        public override SyntaxKind FlipBracket(SyntaxKind bracket)
        {
            switch (bracket)
            {
                case SyntaxKind.LeftBracket:
                    return SyntaxKind.RightBracket;
                case SyntaxKind.OpenAngle:
                    return SyntaxKind.CloseAngle;
                case SyntaxKind.RightBracket:
                    return SyntaxKind.LeftBracket;
                case SyntaxKind.CloseAngle:
                    return SyntaxKind.OpenAngle;
                default:
                    Debug.Fail("FlipBracket must be called with a bracket character");
                    return SyntaxKind.Unknown;
            }
        }

        public override SyntaxToken CreateMarkerToken()
        {
            return SyntaxFactory.Token(SyntaxKind.Unknown, string.Empty);
        }

        public override SyntaxKind GetKnownTokenType(KnownTokenType type)
        {
            switch (type)
            {
                case KnownTokenType.CommentStart:
                    return SyntaxKind.RazorCommentTransition;
                case KnownTokenType.CommentStar:
                    return SyntaxKind.RazorCommentStar;
                case KnownTokenType.CommentBody:
                    return SyntaxKind.RazorCommentLiteral;
                case KnownTokenType.Identifier:
                    return SyntaxKind.HtmlTextLiteral;
                case KnownTokenType.Keyword:
                    return SyntaxKind.HtmlTextLiteral;
                case KnownTokenType.NewLine:
                    return SyntaxKind.NewLine;
                case KnownTokenType.Transition:
                    return SyntaxKind.Transition;
                case KnownTokenType.WhiteSpace:
                    return SyntaxKind.Whitespace;
                default:
                    return SyntaxKind.Unknown;
            }
        }

        protected override SyntaxToken CreateToken(string content, SyntaxKind kind, IReadOnlyList<RazorDiagnostic> errors)
        {
            return SyntaxFactory.Token(kind, content, errors);
        }
    }
}
