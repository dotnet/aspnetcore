// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Razor.Editor;
using Microsoft.AspNet.Razor.Chunks.Generators;
using Microsoft.AspNet.Razor.Text;
using Microsoft.AspNet.Razor.Tokenizer.Symbols;

namespace Microsoft.AspNet.Razor.Parser.SyntaxTree
{
    public class SpanBuilder
    {
        private List<ISymbol> _symbols;
        private SourceLocationTracker _tracker = new SourceLocationTracker();

        public SpanBuilder(Span original)
        {
            Kind = original.Kind;
            _symbols = new List<ISymbol>(original.Symbols);
            EditHandler = original.EditHandler;
            ChunkGenerator = original.ChunkGenerator;
            Start = original.Start;
        }

        public SpanBuilder()
        {
            Reset();
        }

        public SourceLocation Start { get; set; }

        public SpanKind Kind { get; set; }

        public IReadOnlyList<ISymbol> Symbols
        {
            get
            {
                if (_symbols == null)
                {
                    _symbols = new List<ISymbol>();
                }

                return _symbols;
            }
        }

        public SpanEditHandler EditHandler { get; set; }
        public ISpanChunkGenerator ChunkGenerator { get; set; }

        public void Reset()
        {
            // Need to potentially allocate a new list because Span.ReplaceWith takes ownership
            // of the original list.
            _symbols = null;

            EditHandler = SpanEditHandler.CreateDefault(s => Enumerable.Empty<ISymbol>());
            ChunkGenerator = SpanChunkGenerator.Null;
            Start = SourceLocation.Zero;
        }

        public Span Build()
        {
            return new Span(this);
        }

        public void ClearSymbols()
        {
            _symbols.Clear();
        }

        // Short-cut method for adding a symbol
        public void Accept(ISymbol symbol)
        {
            if (symbol == null)
            {
                return;
            }

            if (_symbols == null)
            {
                _symbols = new List<ISymbol>();
            }

            if (_symbols.Count == 0)
            {
                Start = symbol.Start;
                symbol.ChangeStart(SourceLocation.Zero);
                _tracker.CurrentLocation = SourceLocation.Zero;
            }
            else
            {
                symbol.ChangeStart(_tracker.CurrentLocation);
            }

            _symbols.Add(symbol);
            _tracker.UpdateLocation(symbol.Content);
        }
    }
}
