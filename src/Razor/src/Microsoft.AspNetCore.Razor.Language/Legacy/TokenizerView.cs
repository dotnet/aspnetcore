// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    internal class TokenizerView<TTokenizer, TToken, TTokenType>
        where TTokenType : struct
        where TTokenizer : Tokenizer<TToken, TTokenType>
        where TToken : TokenBase<TTokenType>
    {
        public TokenizerView(TTokenizer tokenizer)
        {
            Tokenizer = tokenizer;
        }

        public TTokenizer Tokenizer { get; private set; }
        public bool EndOfFile { get; private set; }
        public TToken Current { get; private set; }

        public ITextDocument Source
        {
            get { return Tokenizer.Source; }
        }

        public bool Next()
        {
            Current = Tokenizer.NextToken();
            EndOfFile = (Current == null);
            return !EndOfFile;
        }

        public void PutBack(TToken token)
        {
            Source.Position -= token.Content.Length;
            Current = null;
            EndOfFile = Source.Position >= Source.Length;
            Tokenizer.Reset();
        }
    }
}
