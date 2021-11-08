// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Razor.Language.Components;

namespace Microsoft.AspNetCore.Razor.Language;

public static class TagHelperDescriptorExtensions
{
    public static string GetTypeName(this TagHelperDescriptor tagHelper)
    {
        if (tagHelper == null)
        {
            throw new ArgumentNullException(nameof(tagHelper));
        }

        tagHelper.Metadata.TryGetValue(TagHelperMetadata.Common.TypeName, out var typeName);
        return typeName;
    }

    public static bool IsDefaultKind(this TagHelperDescriptor tagHelper)
    {
        if (tagHelper == null)
        {
            throw new ArgumentNullException(nameof(tagHelper));
        }

        return string.Equals(tagHelper.Kind, TagHelperConventions.DefaultKind, StringComparison.Ordinal);
    }

    public static bool KindUsesDefaultTagHelperRuntime(this TagHelperDescriptor tagHelper)
    {
        if (tagHelper == null)
        {
            throw new ArgumentNullException(nameof(tagHelper));
        }

        tagHelper.Metadata.TryGetValue(TagHelperMetadata.Runtime.Name, out var value);
        return string.Equals(TagHelperConventions.DefaultKind, value, StringComparison.Ordinal);
    }

    public static bool IsComponentOrChildContentTagHelper(this TagHelperDescriptor tagHelper)
    {
        if (tagHelper == null)
        {
            throw new ArgumentNullException(nameof(tagHelper));
        }

        if (tagHelper.IsComponentTagHelper())
        {
            return true;
        }

        if (tagHelper.IsChildContentTagHelper())
        {
            return true;
        }

        return false;
    }
}
