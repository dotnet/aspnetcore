// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language.Legacy;

internal static class RazorSyntaxTreeExtensions
{
    public static IReadOnlyList<ClassifiedSpanInternal> GetClassifiedSpans(this RazorSyntaxTree syntaxTree)
    {
        if (syntaxTree == null)
        {
            throw new ArgumentNullException(nameof(syntaxTree));
        }

        var visitor = new ClassifiedSpanVisitor(syntaxTree.Source);
        visitor.Visit(syntaxTree.Root);

        return visitor.ClassifiedSpans;
    }

    public static IReadOnlyList<TagHelperSpanInternal> GetTagHelperSpans(this RazorSyntaxTree syntaxTree)
    {
        if (syntaxTree == null)
        {
            throw new ArgumentNullException(nameof(syntaxTree));
        }

        var visitor = new TagHelperSpanVisitor(syntaxTree.Source);
        visitor.Visit(syntaxTree.Root);

        return visitor.TagHelperSpans;
    }
}
