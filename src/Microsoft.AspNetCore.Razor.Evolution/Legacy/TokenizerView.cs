// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;

namespace Microsoft.AspNetCore.Razor.Evolution.Legacy
{
    internal class TokenizerView<TTokenizer, TSymbol, TSymbolType>
        where TSymbolType : struct
        where TTokenizer : Tokenizer<TSymbol, TSymbolType>
        where TSymbol : SymbolBase<TSymbolType>
    {
        public TokenizerView(TTokenizer tokenizer)
        {
            Tokenizer = tokenizer;
        }

        public TTokenizer Tokenizer { get; private set; }
        public bool EndOfFile { get; private set; }
        public TSymbol Current { get; private set; }

        public ITextDocument Source
        {
            get { return Tokenizer.Source; }
        }

        public bool Next()
        {
            Current = Tokenizer.NextSymbol();
            EndOfFile = (Current == null);
            return !EndOfFile;
        }

        public void PutBack(TSymbol symbol)
        {
            Debug.Assert(Source.Position == symbol.Start.AbsoluteIndex + symbol.Content.Length);
            if (Source.Position != symbol.Start.AbsoluteIndex + symbol.Content.Length)
            {
                // We've already passed this symbol
                throw new InvalidOperationException(
                    LegacyResources.FormatTokenizerView_CannotPutBack(
                        symbol.Start.AbsoluteIndex + symbol.Content.Length,
                        Source.Position));
            }
            Source.Position -= symbol.Content.Length;
            Current = null;
            EndOfFile = Source.Position >= Source.Length;
            Tokenizer.Reset();
        }
    }
}
