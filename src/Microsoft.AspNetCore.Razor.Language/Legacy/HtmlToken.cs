// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language.Syntax.InternalSyntax;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    internal class HtmlToken : TokenBase<HtmlTokenType>
    {
        internal static readonly HtmlToken Hyphen = new HtmlToken("-", HtmlTokenType.Text);

        public HtmlToken(string content, HtmlTokenType type)
            : base(content, type, RazorDiagnostic.EmptyArray)
        {
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }
        }

        public HtmlToken(
            string content,
            HtmlTokenType type,
            IReadOnlyList<RazorDiagnostic> errors)
            : base(content, type, errors)
        {
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }
        }

        protected override SyntaxToken GetSyntaxToken()
        {
            switch (Type)
            {
                case HtmlTokenType.Text:
                    return SyntaxFactory.HtmlTextToken(Content, Errors.ToArray());
                case HtmlTokenType.WhiteSpace:
                    return SyntaxFactory.WhitespaceToken(Content, Errors.ToArray());
                case HtmlTokenType.NewLine:
                    return SyntaxFactory.NewLineToken(Content, Errors.ToArray());
                case HtmlTokenType.OpenAngle:
                    return SyntaxFactory.Punctuation(SyntaxKind.OpenAngle, Content, Errors.ToArray());
                case HtmlTokenType.Bang:
                    return SyntaxFactory.Punctuation(SyntaxKind.Bang, Content, Errors.ToArray());
                case HtmlTokenType.ForwardSlash:
                    return SyntaxFactory.Punctuation(SyntaxKind.ForwardSlash, Content, Errors.ToArray());
                case HtmlTokenType.QuestionMark:
                    return SyntaxFactory.Punctuation(SyntaxKind.QuestionMark, Content, Errors.ToArray());
                case HtmlTokenType.DoubleHyphen:
                    return SyntaxFactory.Punctuation(SyntaxKind.DoubleHyphen, Content, Errors.ToArray());
                case HtmlTokenType.LeftBracket:
                    return SyntaxFactory.Punctuation(SyntaxKind.LeftBracket, Content, Errors.ToArray());
                case HtmlTokenType.CloseAngle:
                    return SyntaxFactory.Punctuation(SyntaxKind.CloseAngle, Content, Errors.ToArray());
                case HtmlTokenType.RightBracket:
                    return SyntaxFactory.Punctuation(SyntaxKind.RightBracket, Content, Errors.ToArray());
                case HtmlTokenType.Equals:
                    return SyntaxFactory.Punctuation(SyntaxKind.Equals, Content, Errors.ToArray());
                case HtmlTokenType.DoubleQuote:
                    return SyntaxFactory.Punctuation(SyntaxKind.DoubleQuote, Content, Errors.ToArray());
                case HtmlTokenType.SingleQuote:
                    return SyntaxFactory.Punctuation(SyntaxKind.SingleQuote, Content, Errors.ToArray());
                case HtmlTokenType.Transition:
                    return SyntaxFactory.Punctuation(SyntaxKind.Transition, Content, Errors.ToArray());
                case HtmlTokenType.Colon:
                    return SyntaxFactory.Punctuation(SyntaxKind.Colon, Content, Errors.ToArray());
                default:
                    return SyntaxFactory.UnknownToken(Content, Errors.ToArray());
            }
        }
    }
}
