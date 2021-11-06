// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions.Version1_X;

public static class TagHelperDescriptorExtensions
{
    /// <summary>
    /// Indicates whether a <see cref="TagHelperDescriptor"/> represents a view component.
    /// </summary>
    /// <param name="tagHelper">The <see cref="TagHelperDescriptor"/> to check.</param>
    /// <returns>Whether a <see cref="TagHelperDescriptor"/> represents a view component.</returns>
    public static bool IsViewComponentKind(this TagHelperDescriptor tagHelper)
    {
        if (tagHelper == null)
        {
            throw new ArgumentNullException(nameof(tagHelper));
        }

        return string.Equals(ViewComponentTagHelperConventions.Kind, tagHelper.Kind, StringComparison.Ordinal);
    }

    public static string GetViewComponentName(this TagHelperDescriptor tagHelper)
    {
        if (tagHelper == null)
        {
            throw new ArgumentNullException(nameof(tagHelper));
        }

        tagHelper.Metadata.TryGetValue(ViewComponentTagHelperMetadata.Name, out var result);
        return result;
    }
}
