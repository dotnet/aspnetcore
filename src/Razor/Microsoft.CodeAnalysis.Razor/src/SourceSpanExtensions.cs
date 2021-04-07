// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Razor
{
    internal static class SourceSpanExtensions
    {
        public static TextSpan AsTextSpan(this SourceSpan sourceSpan)
        {
            return new TextSpan(sourceSpan.AbsoluteIndex, sourceSpan.Length);
        }
    }
}
