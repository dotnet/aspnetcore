// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.Text;

namespace Microsoft.AspNet.Razor.Tokenizer.Symbols
{
    public static class SymbolExtensions
    {
        public static LocationTagged<string> GetContent(this SpanBuilder span)
        {
            return GetContent(span, e => e);
        }

        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Justification = "Func<T> is the recommended type for generic delegates and requires this level of nesting")]
        public static LocationTagged<string> GetContent(this SpanBuilder span, Func<IEnumerable<ISymbol>, IEnumerable<ISymbol>> filter)
        {
            return GetContent(filter(span.Symbols), span.Start);
        }

        public static LocationTagged<string> GetContent(this IEnumerable<ISymbol> symbols, SourceLocation spanStart)
        {
            if (symbols.Any())
            {
                return new LocationTagged<string>(String.Concat(symbols.Select(s => s.Content)), spanStart + symbols.First().Start);
            }
            else
            {
                return new LocationTagged<string>(String.Empty, spanStart);
            }
        }

        public static LocationTagged<string> GetContent(this ISymbol symbol)
        {
            return new LocationTagged<string>(symbol.Content, symbol.Start);
        }
    }
}
