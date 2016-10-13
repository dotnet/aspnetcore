// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Evolution.Legacy
{
    internal class HtmlSymbol : SymbolBase<HtmlSymbolType>
    {
        public HtmlSymbol(int absoluteIndex, int lineIndex, int characterIndex, string content, HtmlSymbolType type)
            : this(new SourceLocation(absoluteIndex, lineIndex, characterIndex), content, type, RazorError.EmptyArray)
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
            int absoluteIndex,
            int lineIndex,
            int characterIndex,
            string content,
            HtmlSymbolType type,
            IReadOnlyList<RazorError> errors)
            : base(new SourceLocation(absoluteIndex, lineIndex, characterIndex), content, type, errors)
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
