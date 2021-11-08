// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.AspNetCore.Razor.Language;

public static class TagHelperDescriptorBuilderExtensions
{
    public static void SetTypeName(this TagHelperDescriptorBuilder builder, string typeName)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (typeName == null)
        {
            throw new ArgumentNullException(nameof(typeName));
        }

        builder.Metadata[TagHelperMetadata.Common.TypeName] = typeName;
    }

    public static string GetTypeName(this TagHelperDescriptorBuilder builder)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (builder.Metadata.ContainsKey(TagHelperMetadata.Common.TypeName))
        {
            return builder.Metadata[TagHelperMetadata.Common.TypeName];
        }

        return null;
    }
}
