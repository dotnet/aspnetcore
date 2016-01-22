// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Chunks.Generators;
using Microsoft.AspNetCore.Razor.Editor;
using Microsoft.AspNetCore.Razor.Parser.SyntaxTree;
using Microsoft.AspNetCore.Razor.Tokenizer.Symbols;

namespace Microsoft.AspNetCore.Mvc.Razor
{
    public class SpanConstructor
    {
        public SpanBuilder Builder { get; private set; }

        internal static IEnumerable<ISymbol> TestTokenizer(string str)
        {
            yield return new RawTextSymbol(SourceLocation.Zero, str);
        }

        public SpanConstructor(SpanKind kind, IEnumerable<ISymbol> symbols)
        {
            Builder = new SpanBuilder();
            Builder.Kind = kind;
            Builder.EditHandler = SpanEditHandler.CreateDefault(TestTokenizer);
            foreach (ISymbol sym in symbols)
            {
                Builder.Accept(sym);
            }
        }

        private Span Build()
        {
            return Builder.Build();
        }

        public SpanConstructor With(ISpanChunkGenerator generator)
        {
            Builder.ChunkGenerator = generator;
            return this;
        }

        public SpanConstructor With(SpanEditHandler handler)
        {
            Builder.EditHandler = handler;
            return this;
        }

        public SpanConstructor With(Action<ISpanChunkGenerator> generatorConfigurer)
        {
            generatorConfigurer(Builder.ChunkGenerator);
            return this;
        }

        public SpanConstructor With(Action<SpanEditHandler> handlerConfigurer)
        {
            handlerConfigurer(Builder.EditHandler);
            return this;
        }

        public static implicit operator Span(SpanConstructor self)
        {
            return self.Build();
        }

        public SpanConstructor Hidden()
        {
            Builder.ChunkGenerator = SpanChunkGenerator.Null;
            return this;
        }

        public SpanConstructor Accepts(AcceptedCharacters accepted)
        {
            return With(eh => eh.AcceptedCharacters = accepted);
        }
    }
}