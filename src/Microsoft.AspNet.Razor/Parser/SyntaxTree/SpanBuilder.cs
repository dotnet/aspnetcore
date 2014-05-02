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

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.AspNet.Razor.Editor;
using Microsoft.AspNet.Razor.Generator;
using Microsoft.AspNet.Razor.Text;
using Microsoft.AspNet.Razor.Tokenizer.Symbols;

namespace Microsoft.AspNet.Razor.Parser.SyntaxTree
{
    public class SpanBuilder
    {
        private IList<ISymbol> _symbols = new List<ISymbol>();
        private SourceLocationTracker _tracker = new SourceLocationTracker();

        public SpanBuilder(Span original)
        {
            Kind = original.Kind;
            _symbols = new List<ISymbol>(original.Symbols);
            EditHandler = original.EditHandler;
            CodeGenerator = original.CodeGenerator;
            Start = original.Start;
        }

        public SpanBuilder()
        {
            Reset();
        }

        public SourceLocation Start { get; set; }
        public SpanKind Kind { get; set; }

        public ReadOnlyCollection<ISymbol> Symbols
        {
            get { return new ReadOnlyCollection<ISymbol>(_symbols); }
        }

        public SpanEditHandler EditHandler { get; set; }
        public ISpanCodeGenerator CodeGenerator { get; set; }

        public void Reset()
        {
            _symbols = new List<ISymbol>();
            EditHandler = SpanEditHandler.CreateDefault(s => Enumerable.Empty<ISymbol>());
            CodeGenerator = SpanCodeGenerator.Null;
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
