// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

namespace Microsoft.AspNet.Razor.Tokenizer.Symbols
{
    public enum CSharpSymbolType
    {
        Unknown,
        Identifier,
        Keyword,
        IntegerLiteral,
        NewLine,
        WhiteSpace,
        Comment,
        RealLiteral,
        CharacterLiteral,
        StringLiteral,

        // Operators
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
        Colon,
        Semicolon,
        QuestionMark,
        NullCoalesce,
        RightBracket,
        LeftBracket,
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
        Equals,
        GreaterThan,
        GreaterThanEqual,
        RightShift,
        RightShiftAssign,
        Hash,
        Transition,

        // Razor specific
        RazorCommentTransition,
        RazorCommentStar,
        RazorComment
    }
}
