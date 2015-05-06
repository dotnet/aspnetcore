// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Razor.Tokenizer.Symbols
{
    public class HtmlSymbol : SymbolBase<HtmlSymbolType>
    {
        // Helper constructor
        public HtmlSymbol(int offset, int line, int column, [NotNull] string content, HtmlSymbolType type)
            : this(new SourceLocation(offset, line, column), content, type, Enumerable.Empty<RazorError>())
        {
        }

        public HtmlSymbol(SourceLocation start, [NotNull] string content, HtmlSymbolType type)
            : base(start, content, type, Enumerable.Empty<RazorError>())
        {
        }

        public HtmlSymbol(
            int offset,
            int line,
            int column,
            [NotNull] string content,
            HtmlSymbolType type,
            IEnumerable<RazorError> errors)
            : base(new SourceLocation(offset, line, column), content, type, errors)
        {
        }

        public HtmlSymbol(
            SourceLocation start,
            [NotNull] string content,
            HtmlSymbolType type,
            IEnumerable<RazorError> errors)
            : base(start, content, type, errors)
        {
        }
    }
}
