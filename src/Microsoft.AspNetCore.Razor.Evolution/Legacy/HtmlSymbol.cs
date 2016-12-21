// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Evolution.Legacy
{
    internal class HtmlSymbol : SymbolBase<HtmlSymbolType>
    {
        public HtmlSymbol(string content, HtmlSymbolType type)
            : base(content, type, RazorError.EmptyArray)
        {
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }
        }

        public HtmlSymbol(
            string content,
            HtmlSymbolType type,
            IReadOnlyList<RazorError> errors)
            : base(content, type, errors)
        {
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }
        }
    }
}
