// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.Language
{
    internal enum SyntaxKind : byte
    {
        #region Nodes
        HtmlText,
        HtmlDocument,
        HtmlDeclaration,
        #endregion

        #region Tokens
        // Common
        Unknown,
        List,
        Whitespace,
        NewLine,
        Colon,
        QuestionMark,
        RightBracket,
        LeftBracket,
        Equals,
        Transition,

        // HTML
        HtmlTextLiteral,
        OpenAngle,
        Bang,
        ForwardSlash,
        DoubleHyphen,
        CloseAngle,
        DoubleQuote,
        SingleQuote,

        // CSharp literals
        Identifier,
        Keyword,
        IntegerLiteral,
        CSharpComment,
        RealLiteral,
        CharacterLiteral,
        StringLiteral,

        // CSharp operators
        Arrow,
        Minus,
        Decrement,
        MinusAssign,
        NotEqual,
        Not,
        Modulo,
        ModuloAssign,
        AndAssign,
        And,
        DoubleAnd,
        LeftParenthesis,
        RightParenthesis,
        Star,
        MultiplyAssign,
        Comma,
        Dot,
        Slash,
        DivideAssign,
        DoubleColon,
        Semicolon,
        NullCoalesce,
        XorAssign,
        Xor,
        LeftBrace,
        OrAssign,
        DoubleOr,
        Or,
        RightBrace,
        Tilde,
        Plus,
        PlusAssign,
        Increment,
        LessThan,
        LessThanEqual,
        LeftShift,
        LeftShiftAssign,
        Assign,
        GreaterThan,
        GreaterThanEqual,
        RightShift,
        RightShiftAssign,
        Hash,

        // Razor specific
        RazorComment,
        RazorCommentStar,
        RazorCommentTransition,
        #endregion
    }
}
