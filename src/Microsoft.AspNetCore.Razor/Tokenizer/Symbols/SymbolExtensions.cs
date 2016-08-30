// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Razor.Parser.SyntaxTree;
using Microsoft.AspNetCore.Razor.Text;

namespace Microsoft.AspNetCore.Razor.Tokenizer.Symbols
{
    public static class SymbolExtensions
    {
        public static LocationTagged<string> GetContent(this SpanBuilder span)
        {
            var symbols = span.Symbols;
            if (symbols.Count > 0)
            {
                var text = new StringBuilder();
                for (var i = 0; i < symbols.Count; i++)
                {
                    text.Append(symbols[i].Content);
                }

                return new LocationTagged<string>(text.ToString(), span.Start + symbols[0].Start);
            }
            else
            {
                return new LocationTagged<string>(string.Empty, span.Start);
            }
        }

        public static LocationTagged<string> GetContent(this SpanBuilder span, Func<IEnumerable<ISymbol>, IEnumerable<ISymbol>> filter)
        {
            return GetContent(filter(span.Symbols), span.Start);
        }

        public static LocationTagged<string> GetContent(this IEnumerable<ISymbol> symbols, SourceLocation spanStart)
        {
            StringBuilder builder = null;
            var location = spanStart;

            foreach (var symbol in symbols)
            {
                if (builder == null)
                {
                    builder = new StringBuilder();
                    location += symbol.Start;
                }

                builder.Append(symbol.Content);
            }

            return new LocationTagged<string>(builder?.ToString() ?? string.Empty, location);
        }

        public static LocationTagged<string> GetContent(this ISymbol symbol)
        {
            return new LocationTagged<string>(symbol.Content, symbol.Start);
        }
    }
}
