// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    internal class HtmlSymbol : SymbolBase<HtmlSymbolType>
    {
        internal static readonly HtmlSymbol Hyphen = new HtmlSymbol("-", HtmlSymbolType.Text);

        public HtmlSymbol(string content, HtmlSymbolType type)
            : base(content, type, RazorDiagnostic.EmptyArray)
        {
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }
        }

        public HtmlSymbol(
            string content,
            HtmlSymbolType type,
            IReadOnlyList<RazorDiagnostic> errors)
            : base(content, type, errors)
        {
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }
        }
    }
}