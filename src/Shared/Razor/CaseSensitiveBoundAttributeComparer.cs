// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore.Html;

namespace Microsoft.AspNetCore.Razor.TagHelpers.Testing;

internal sealed class CaseSensitiveTagHelperAttributeComparer : IEqualityComparer<TagHelperAttribute>
{
    public static readonly CaseSensitiveTagHelperAttributeComparer Default =
        new CaseSensitiveTagHelperAttributeComparer();

    private CaseSensitiveTagHelperAttributeComparer()
    {
    }

    public bool Equals(TagHelperAttribute attributeX, TagHelperAttribute attributeY)
    {
        if (attributeX == attributeY)
        {
            return true;
        }

        // Normal comparer (TagHelperAttribute.Equals()) doesn't care about the Name case, in tests we do.
        return attributeX != null &&
            string.Equals(attributeX.Name, attributeY.Name, StringComparison.Ordinal) &&
            attributeX.ValueStyle == attributeY.ValueStyle &&
            (attributeX.ValueStyle == HtmlAttributeValueStyle.Minimized ||
             string.Equals(GetString(attributeX.Value), GetString(attributeY.Value)));
    }

    public int GetHashCode(TagHelperAttribute attribute)
    {
        return attribute.GetHashCode();
    }

    private static string GetString(object value)
    {
        var htmlContent = value as IHtmlContent;
        if (htmlContent != null)
        {
            using (var writer = new StringWriter())
            {
                htmlContent.WriteTo(writer, NullHtmlEncoder.Default);
                return writer.ToString();
            }
        }

        return value?.ToString() ?? string.Empty;
    }
}
