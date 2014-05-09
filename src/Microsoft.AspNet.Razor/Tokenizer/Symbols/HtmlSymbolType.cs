// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Razor.Tokenizer.Symbols
{
    public enum HtmlSymbolType
    {
        Unknown,
        Text, // Text which isn't one of the below
        WhiteSpace, // Non-newline Whitespace
        NewLine, // Newline
        OpenAngle, // <
        Bang, // !
        Solidus, // /
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
