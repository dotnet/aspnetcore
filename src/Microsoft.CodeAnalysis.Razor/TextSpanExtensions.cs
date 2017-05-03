// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Razor
{
    internal static class TextSpanExtensions
    {
        public static SourceSpan AsSourceSpan(this TextSpan textSpan)
        {
            return new SourceSpan(filePath: null, absoluteIndex: textSpan.Start, lineIndex: -1, characterIndex: -1, length: textSpan.Length);
        }
    }
}
