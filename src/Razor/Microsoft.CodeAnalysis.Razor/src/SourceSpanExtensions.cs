// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Razor;

internal static class SourceSpanExtensions
{
    public static TextSpan AsTextSpan(this SourceSpan sourceSpan)
    {
        return new TextSpan(sourceSpan.AbsoluteIndex, sourceSpan.Length);
    }
}
