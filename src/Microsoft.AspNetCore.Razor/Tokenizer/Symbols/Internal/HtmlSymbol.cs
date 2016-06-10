// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.Razor.Tokenizer.Symbols.Internal
{
    public class HtmlSymbol : SymbolBase<HtmlSymbolType>
    {
        // Helper constructor
        public HtmlSymbol(int offset, int line, int column, string content, HtmlSymbolType type)
            : this(new SourceLocation(offset, line, column), content, type, RazorError.EmptyArray)
        {
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }
        }

        public HtmlSymbol(SourceLocation start, string content, HtmlSymbolType type)
            : base(start, content, type, RazorError.EmptyArray)
        {
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }
        }

        public HtmlSymbol(
            int offset,
            int line,
            int column,
            string content,
            HtmlSymbolType type,
            IReadOnlyList<RazorError> errors)
            : base(new SourceLocation(offset, line, column), content, type, errors)
        {
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }
        }

        public HtmlSymbol(
            SourceLocation start,
            string content,
            HtmlSymbolType type,
            IReadOnlyList<RazorError> errors)
            : base(start, content, type, errors)
        {
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }
        }
    }
}
