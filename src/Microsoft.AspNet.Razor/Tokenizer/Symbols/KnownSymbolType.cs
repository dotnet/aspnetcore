// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Razor.Tokenizer.Symbols
{
    public enum KnownSymbolType
    {
        WhiteSpace,
        NewLine,
        Identifier,
        Keyword,
        Transition,
        Unknown,
        CommentStart,
        CommentStar,
        CommentBody
    }
}
