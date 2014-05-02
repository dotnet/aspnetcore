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
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.Text;
using Microsoft.AspNet.Razor.Tokenizer;
using Microsoft.AspNet.Razor.Tokenizer.Symbols;

namespace Microsoft.AspNet.Razor.Parser
{
    [SuppressMessage("Microsoft.Design", "CA1005:AvoidExcessiveParametersOnGenericTypes", Justification = "All generic type parameters are required")]
    public abstract partial class TokenizerBackedParser<TTokenizer, TSymbol, TSymbolType> : ParserBase
        where TTokenizer : Tokenizer<TSymbol, TSymbolType>
        where TSymbol : SymbolBase<TSymbolType>
    {
        private TokenizerView<TTokenizer, TSymbol, TSymbolType> _tokenizer;

        protected TokenizerBackedParser()
        {
            Span = new SpanBuilder();
        }

        protected SpanBuilder Span { get; set; }

        protected TokenizerView<TTokenizer, TSymbol, TSymbolType> Tokenizer
        {
            get { return _tokenizer ?? InitTokenizer(); }
        }

        protected Action<SpanBuilder> SpanConfig { get; set; }

        protected TSymbol CurrentSymbol
        {
            get { return Tokenizer.Current; }
        }

        protected TSymbol PreviousSymbol { get; private set; }

        protected SourceLocation CurrentLocation
        {
            get { return (EndOfFile || CurrentSymbol == null) ? Context.Source.Location : CurrentSymbol.Start; }
        }

        protected bool EndOfFile
        {
            get { return Tokenizer.EndOfFile; }
        }

        protected abstract LanguageCharacteristics<TTokenizer, TSymbol, TSymbolType> Language { get; }

        protected virtual void HandleEmbeddedTransition()
        {
        }

        protected virtual bool IsAtEmbeddedTransition(bool allowTemplatesAndComments, bool allowTransitions)
        {
            return false;
        }

        public override void BuildSpan(SpanBuilder span, SourceLocation start, string content)
        {
            foreach (ISymbol sym in Language.TokenizeString(start, content))
            {
                span.Accept(sym);
            }
        }

        protected void Initialize(SpanBuilder span)
        {
            if (SpanConfig != null)
            {
                SpanConfig(span);
            }
        }

        protected internal bool NextToken()
        {
            PreviousSymbol = CurrentSymbol;
            return Tokenizer.Next();
        }

        private TokenizerView<TTokenizer, TSymbol, TSymbolType> InitTokenizer()
        {
            return _tokenizer = new TokenizerView<TTokenizer, TSymbol, TSymbolType>(Language.CreateTokenizer(Context.Source));
        }
    }
}
