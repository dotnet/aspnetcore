// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Razor.Editor;
using Microsoft.AspNet.Razor.Generator;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.Text;
using Microsoft.AspNet.Razor.Tokenizer.Symbols;

namespace Microsoft.AspNet.Mvc.Razor
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

        public SpanConstructor With(ISpanCodeGenerator generator)
        {
            Builder.CodeGenerator = generator;
            return this;
        }

        public SpanConstructor With(SpanEditHandler handler)
        {
            Builder.EditHandler = handler;
            return this;
        }

        public SpanConstructor With(Action<ISpanCodeGenerator> generatorConfigurer)
        {
            generatorConfigurer(Builder.CodeGenerator);
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
            Builder.CodeGenerator = SpanCodeGenerator.Null;
            return this;
        }

        public SpanConstructor Accepts(AcceptedCharacters accepted)
        {
            return With(eh => eh.AcceptedCharacters = accepted);
        }
    }
}