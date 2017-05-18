// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    internal class SpanBuilder
    {
        private SourceLocation _start;
        private List<ISymbol> _symbols;
        private SourceLocationTracker _tracker;

        public SpanBuilder(Span original)
        {
            Kind = original.Kind;
            EditHandler = original.EditHandler;
            _start = original.Start;
            ChunkGenerator = original.ChunkGenerator;

            _symbols = new List<ISymbol>(original.Symbols);
            _tracker = new SourceLocationTracker(original.Start);
        }

        public SpanBuilder(SourceLocation location)
        {
            _tracker = new SourceLocationTracker();

            Reset();

            Start = location;
        }

        public ISpanChunkGenerator ChunkGenerator { get; set; }

        public SourceLocation Start
        {
            get { return _start; }
            set
            {
                _start = value;
                _tracker.CurrentLocation = value;
            }
        }

        public SourceLocation End => _tracker.CurrentLocation;

        public SpanKindInternal Kind { get; set; }

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

        public void Reset()
        {
            // Need to potentially allocate a new list because Span.ReplaceWith takes ownership
            // of the original list.
            _symbols = null;
            _symbols = new List<ISymbol>();

            EditHandler = SpanEditHandler.CreateDefault((content) => Enumerable.Empty<ISymbol>());
            ChunkGenerator = SpanChunkGenerator.Null;
            Start = SourceLocation.Undefined;
        }

        public Span Build()
        {
            var span = new Span(this);
            
            for (var i = 0; i < span.Symbols.Count; i++)
            {
                var symbol = span.Symbols[i];
                symbol.Parent = span;
            }

            return span;
        }

        public void ClearSymbols()
        {
            _symbols?.Clear();
        }

        public void Accept(ISymbol symbol)
        {
            if (symbol == null)
            {
                return;
            }

            if (Start.Equals(SourceLocation.Undefined))
            {
                throw new InvalidOperationException("SpanBuilder must have a valid location");
            }

            _symbols.Add(symbol);
            _tracker.UpdateLocation(symbol.Content);
        }
    }
}
