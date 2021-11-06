// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate;

internal static class HtmlContentIntermediateNodeExtensions
{
    private const string HasEncodedContent = "HasEncodedContent";

    public static bool IsEncoded(this HtmlContentIntermediateNode node)
    {
        return ReferenceEquals(node.Annotations[HasEncodedContent], HasEncodedContent);
    }

    public static void SetEncoded(this HtmlContentIntermediateNode node)
    {
        if (node == null)
        {
            throw new ArgumentNullException(nameof(node));
        }

        node.Annotations[HasEncodedContent] = HasEncodedContent;
    }
}
