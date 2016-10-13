// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.Razor.Evolution.Legacy
{
    internal static class SymbolExtensions
    {
        public static LocationTagged<string> GetContent(this SpanBuilder span)
        {
            return GetContent(span, e => e);
        }

        public static LocationTagged<string> GetContent(this SpanBuilder span, Func<IEnumerable<ISymbol>, IEnumerable<ISymbol>> filter)
        {
            return GetContent(filter(span.Symbols), span.Start);
        }

        public static LocationTagged<string> GetContent(this IEnumerable<ISymbol> symbols, SourceLocation spanStart)
        {
            if (symbols.Any())
            {
                return new LocationTagged<string>(string.Concat(symbols.Select(s => s.Content)), spanStart + symbols.First().Start);
            }
            else
            {
                return new LocationTagged<string>(string.Empty, spanStart);
            }
        }

        public static LocationTagged<string> GetContent(this ISymbol symbol)
        {
            return new LocationTagged<string>(symbol.Content, symbol.Start);
        }
    }
}
