// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    [Flags]
    internal enum HtmlSymbolType
    {
        Unknown,
        Text, // Text which isn't one of the below
        WhiteSpace, // Non-newline Whitespace
        NewLine, // Newline
        OpenAngle, // <
        Bang, // !
        ForwardSlash, // /
        QuestionMark, // ?
        DoubleHyphen, // --
        LeftBracket, // [
        CloseAngle, // >
        RightBracket, // ]
        Equals, // =
        DoubleQuote, // "
        SingleQuote, // '
        Transition, // @
        Colon,
        RazorComment,
        RazorCommentStar,
        RazorCommentTransition
    }
}
