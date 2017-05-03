// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Razor
{
    internal static class TextChangeExtensions
    {
        public static SourceChange AsSourceChange(this TextChange textChange)
        {
            if (textChange == null)
            {
                throw new ArgumentNullException(nameof(textChange));
            }

            return new SourceChange(textChange.Span.AsSourceSpan(), textChange.NewText);
        }
    }
}
