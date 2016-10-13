// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Evolution.Legacy
{
    internal class CSharpSymbol : SymbolBase<CSharpSymbolType>
    {
        public CSharpSymbol(int absoluteIndex, int lineIndex, int characterIndex, string content, CSharpSymbolType type)
            : this(new SourceLocation(absoluteIndex, lineIndex, characterIndex), content, type, RazorError.EmptyArray)
        {
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }
        }

        public CSharpSymbol(SourceLocation start, string content, CSharpSymbolType type)
            : this(start, content, type, RazorError.EmptyArray)
        {
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }
        }

        public CSharpSymbol(
            int offset,
            int line,
            int column,
            string content,
            CSharpSymbolType type,
            IReadOnlyList<RazorError> errors)
            : base(new SourceLocation(offset, line, column), content, type, errors)
        {
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }
        }

        public CSharpSymbol(
            SourceLocation start,
            string content,
            CSharpSymbolType type,
            IReadOnlyList<RazorError> errors)
            : base(start, content, type, errors)
        {
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }
        }

        public CSharpKeyword? Keyword { get; set; }

        public override bool Equals(object obj)
        {
            var other = obj as CSharpSymbol;
            return base.Equals(other) &&
                other.Keyword == Keyword;
        }

        public override int GetHashCode()
        {
            // Hash code should include only immutable properties.
            return base.GetHashCode();
        }
    }
}
