// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.Language
{
    internal enum SyntaxKind : byte
    {
        Unknown,
        List,
        Whitespace,
        NewLine,

        // HTML
        HtmlText,
        HtmlDocument,
        HtmlDeclaration,
        HtmlTextLiteralToken,
        OpenAngle,
        Bang,
        ForwardSlash,
        QuestionMark,
        DoubleHyphen,
        LeftBracket,
        CloseAngle,
        RightBracket,
        Equals,
        DoubleQuote,
        SingleQuote,
        Transition,
        Colon,
    }
}
