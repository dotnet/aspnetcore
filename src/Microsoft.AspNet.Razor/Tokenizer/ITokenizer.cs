// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using Microsoft.AspNet.Razor.Tokenizer.Symbols;

namespace Microsoft.AspNet.Razor.Tokenizer
{
    public interface ITokenizer
    {
        ISymbol NextSymbol();
    }
}
