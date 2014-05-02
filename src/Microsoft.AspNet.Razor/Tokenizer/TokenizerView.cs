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

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.AspNet.Razor.Text;
using Microsoft.AspNet.Razor.Tokenizer.Symbols;

namespace Microsoft.AspNet.Razor.Tokenizer
{
    [SuppressMessage("Microsoft.Design", "CA1005:AvoidExcessiveParametersOnGenericTypes", Justification = "All generic parameters are required")]
    public class TokenizerView<TTokenizer, TSymbol, TSymbolType>
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
                    RazorResources.TokenizerView_CannotPutBack(
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
